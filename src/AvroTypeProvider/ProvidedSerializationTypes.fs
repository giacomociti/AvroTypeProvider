namespace AvroTypeProvider

#nowarn "25"

open System.Reflection
open Microsoft.FSharp.Quotations
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

    let writePrimitiveValue (schema: PrimitiveSchema) encoder value =
        match schema.Tag with
        | Tag.String -> <@@ (%%encoder: Encoder).WriteString(%%value) @@>
        | Tag.Int -> <@@ (%%encoder: Encoder).WriteInt(%%value) @@>
        | Tag.Null -> <@@ (%%encoder: Encoder).WriteNull() @@>
        | Tag.Long -> <@@ (%%encoder: Encoder).WriteLong(%%value) @@>
        | Tag.Float -> <@@ (%%encoder: Encoder).WriteFloat(%%value) @@>
        | Tag.Double -> <@@ (%%encoder: Encoder).WriteDouble(%%value) @@>
        | Tag.Bytes -> <@@ (%%encoder: Encoder).WriteBytes(%%value) @@>
        | Tag.Boolean -> <@@ (%%encoder: Encoder).WriteBoolean(%%value) @@>
        | _ -> failwithf "Unknown schema type: %A" schema.Tag

    let writeArray (schema: ArraySchema) encoder value =
        <@@ // TODO
            (%%encoder: Encoder).WriteArrayStart()
            (%%encoder: Encoder).SetItemCount(0L)
            (%%encoder: Encoder).WriteArrayEnd()
        @@>

    let writeMap (schema: MapSchema) encoder value =
        <@@ // TODO
            (%%encoder: Encoder).WriteMapStart()
            (%%encoder: Encoder).SetItemCount(0L)
            (%%encoder: Encoder).WriteMapEnd()
        @@>


    let rec writeValue schema encoder value =
        match schema with
        | Primitive schema -> writePrimitiveValue schema encoder value
        | Named schema ->
            match schema with
            | Record schema -> <@@ () @@> // TODO
            | Enum schema ->
                <@@ (%%encoder: Encoder).WriteEnum(0) @@> //TODO %%value
            | Fixed schema -> <@@ () @@>
        | Union schema -> 
            match schema with
            | NullOrType schema -> // TODO
                <@@ 
                    (%%encoder: Encoder).WriteUnionIndex(0)
                    (%%encoder: Encoder).WriteNull()
                @@>
                // let v = Expr.DefaultValue value.Type
                // Expr.IfThenElse(
                //     <@@ true @@>,
                //     <@@
                //         (%%encoder: Encoder).WriteUnionIndex(0)
                //         (%%encoder: Encoder).WriteNull()
                //     @@>,
                //     Expr.Sequential(
                //         <@@ (%%encoder: Encoder).WriteUnionIndex(1) @@>,
                //         writeValue schema encoder v) // TODO value.Value
                //     )      
            | TypeOrNull schema -> // TODO
                <@@ 
                    (%%encoder: Encoder).WriteUnionIndex(1)
                    (%%encoder: Encoder).WriteNull()
                @@>
            | RealUnion schema -> <@@ () @@> // TODO
        | Array schema -> writeArray schema encoder value
        | Map schema -> writeMap schema encoder value

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
                    writeValue field.Schema encoder value)
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