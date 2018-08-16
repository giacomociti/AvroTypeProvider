namespace AvroTypeProvider

#nowarn "25"

open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open ProviderImplementation.ProvidedTypes
open Avro
open Avro.IO
open Avro.Generic
open AvroTypes

module ProvidedSerializationTypes =

    let sequential expressions =
        expressions
        |> Seq.reduce (fun e1 e2 -> Expr.Sequential(e1, e2))

    let overrideMethod
        (baseType: System.Type)
        (providedType: ProvidedTypeDefinition)
        (method: ProvidedMethod) =
        MethodAttributes.Virtual
        ||| MethodAttributes.Private
        ||| MethodAttributes.Final
        ||| MethodAttributes.NewSlot
        ||| MethodAttributes.HasSecurity
        |> method.SetMethodAttrs
        providedType.AddMember method
        providedType.DefineMethodOverride(method, baseType.GetMethod(method.Name))

    let writePrimitiveValue (schema: PrimitiveSchema) encoder =
        match schema.Tag with
        | Tag.String -> <@@ fun x -> (%%encoder: Encoder).WriteString(x) @@>
        | Tag.Int -> <@@ fun x -> (%%encoder: Encoder).WriteInt(x) @@>
        | Tag.Null -> <@@ fun _ -> (%%encoder: Encoder).WriteNull() @@>
        | Tag.Long -> <@@ fun x -> (%%encoder: Encoder).WriteLong(x) @@>
        | Tag.Float -> <@@ fun x -> (%%encoder: Encoder).WriteFloat(x) @@>
        | Tag.Double -> <@@ fun x -> (%%encoder: Encoder).WriteDouble(x) @@>
        | Tag.Bytes -> <@@ fun x -> (%%encoder: Encoder).WriteBytes(x) @@>
        | Tag.Boolean -> <@@ fun x -> (%%encoder: Encoder).WriteBoolean(x) @@>
        | _ -> failwithf "Unknown schema type: %A" schema.Tag

    let count itemType items =
        let genericDefinition =
            match <@@ System.Linq.Enumerable.LongCount [] @@> with
            | Call(_, m, _) -> m.GetGenericMethodDefinition()
        let method = genericDefinition.MakeGenericMethod [| itemType |]
        Expr.Call(method, [items])

    let iterate itemType action items =
        let genericDefinition =
            match <@@ Seq.iter (fun _ -> ()) [] @@> with
            | Call(_, m, _) -> m.GetGenericMethodDefinition()
        let method = genericDefinition.MakeGenericMethod [| itemType |]
        Expr.Call(method, [action; items])


    let rec writeValue schema typ encoder =
        match schema with
        | Primitive schema -> writePrimitiveValue schema encoder
        | Named schema ->
            match schema with
            | Record schema -> <@@ () @@> // TODO
            | Enum schema ->
                let x = Var("x", typ)
                Expr.Lambda(x,
                    <@@ (%%encoder: Encoder).WriteEnum(0) @@> )//TODO %%value
            | Fixed schema -> <@@ () @@>
        | Union schema ->
            match schema with
            | NullOrType schema -> // TODO
                let writeNull =
                    <@@
                        (%%encoder: Encoder).WriteUnionIndex(0)
                        (%%encoder: Encoder).WriteNull()
                    @@>
                let innerType = (typ: System.Type).GenericTypeArguments.[0]
                let innerWrite = writeValue schema innerType encoder
                let x = Var("x", typ)

                //let a = Expr.Value (System.Nullable(0))
                //let iterMethodDef =
                //    match <@@ (%%a: System.Nullable<_>).HasValue @@> with
                //    //| Call(_, m, _) -> m.GetGenericMethodDefinition()
                //    | Patterns.PropertyGet(_, p, _) -> p
                //    | x -> failwithf "%A" x

                let hasValue = Expr.PropertyGet(Expr.Var x, typ.GetProperty("HasValue"))
                let value = Expr.PropertyGet(Expr.Var x, typ.GetProperty("Value"))
                let writeValue =
                    Expr.Sequential(
                        <@@ (%%encoder: Encoder).WriteUnionIndex(1) @@>,
                        Expr.Application(innerWrite, value))

                Expr.Lambda(x, Expr.IfThenElse(hasValue, writeValue, writeNull))

            | TypeOrNull schema -> // TODO
                <@@ fun x ->
                        (%%encoder: Encoder).WriteUnionIndex(1)
                        (%%encoder: Encoder).WriteNull()
                @@>
            | RealUnion schema -> <@@ fun x -> () @@> // TODO
        | Array schema ->
            let innerType = (typ: System.Type).GenericTypeArguments.[0]
            let innerWrite = writeValue schema.ItemSchema innerType encoder
            let x = Var("x", typ)
            let n = count innerType (Expr.Var x)
            Expr.Lambda(x,
                [
                    <@@ (%%encoder: Encoder).WriteArrayStart() @@>
                    <@@ (%%encoder: Encoder).SetItemCount(%%n) @@>
                    iterate innerType innerWrite (Expr.Var x)
                    <@@ (%%encoder: Encoder).WriteArrayEnd() @@>
                ] |> sequential)
        | Map schema ->
            let innerType = typ.GenericTypeArguments.[1]
            let innerWriteValue = writeValue schema.ValueSchema innerType encoder
            let genericPair = typedefof<KeyValuePair<_,_>>
            let kvType = genericPair.MakeGenericType [| typeof<string>; innerType |]
            let innerWrite =
                let kv = Var("kv", kvType)
                let key = Expr.PropertyGet(Expr.Var kv, kvType.GetProperty("Key"))
                let value = Expr.PropertyGet(Expr.Var kv, kvType.GetProperty("Value"))
                Expr.Lambda(kv,
                    Expr.Sequential(
                        <@@ (%%encoder: Encoder).WriteString(%%key) @@>,
                        Expr.Application(innerWriteValue, value)))
            let x = Var("x", typ)
            let n = count kvType (Expr.Var x)
            Expr.Lambda(x,
                [
                    <@@ (%%encoder: Encoder).WriteMapStart() @@>
                    <@@ (%%encoder: Encoder).SetItemCount(%%n) @@>
                    iterate kvType innerWrite (Expr.Var x)
                    <@@ (%%encoder: Encoder).WriteMapEnd() @@>
                ] |> sequential)

    let writeMethod providedType (schema: RecordSchema) =
        ProvidedMethod("Write",
            parameters = [ ProvidedParameter("datum", providedType)
                           ProvidedParameter("encoder", typeof<Encoder>)],
            returnType = typeof<System.Void>,
            invokeCode = (fun [writer; datum; encoder] ->
                schema.Fields
                |> Seq.map (fun field ->
                    let property = providedType.GetProperty field.Name
                    let value = Expr.PropertyGet(datum, property)
                    let write = writeValue field.Schema property.PropertyType encoder
                    Expr.Application(write, value))
                |> sequential))

    let setRecordWriter
        (enclosingType: ProvidedTypeDefinition)
        (providedRecordType: ProvidedTypeDefinition)
        (schema: RecordSchema) =

        let writerInterface =
            ProvidedTypeBuilder.MakeGenericType(typedefof<DatumWriter<_>>, [providedRecordType])
        let writerType =
            ProvidedTypeDefinition(
                assembly = enclosingType.Assembly,
                namespaceName = "",
                className = providedRecordType.Name + "DatumWriter",
                baseType = Some typeof<obj>,
                isErased = false,
                hideObjectMethods = true)

        let schemaField = ProvidedField("_schema", typeof<Schema>)
        writerType.AddMember schemaField

        let schemaText = schema.ToString()
        let ctor = ProvidedConstructor([], invokeCode = fun [this] ->
            Expr.FieldSet(this, schemaField, <@@ Schema.Parse schemaText @@>))
        writerType.AddMember ctor

        let schemaProperty =
            ProvidedMethod("get_Schema",
                parameters = [],
                returnType = typeof<Schema>,
                invokeCode = fun [this] -> Expr.FieldGet(this, schemaField))
        [ schemaProperty
          writeMethod providedRecordType schema ]
        |> List.iter (overrideMethod writerInterface writerType)

        writerType.AddInterfaceImplementation writerInterface
        enclosingType.AddMember writerType