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

    let writeValue schema encoder value =
        match schema with
        | Primitive schema -> 
            match schema.Tag with
            | Tag.String -> <@@ (%%encoder: Encoder).WriteString(%%value) @@>
            | Tag.Int -> <@@ (%%encoder: Encoder).WriteInt(%%value) @@>
            // TODO etc.
            | _ -> <@@ () @@>
        | Named schema ->
            match schema with
            | Record schema -> <@@ () @@>
            | Enum schema ->
                <@@ (%%encoder: Encoder).WriteEnum(0) @@> //TODO %%value
            | Fixed schema -> <@@ () @@>
            
        | Union schema -> <@@ () @@>
        | Array schema -> <@@ () @@>
        | Map schema -> <@@ () @@>

        

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