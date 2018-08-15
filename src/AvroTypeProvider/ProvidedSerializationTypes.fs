namespace AvroTypeProvider

#nowarn "25"

open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Avro
open Avro.IO
open System.Reflection
open Avro.Generic

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
                    let o = Expr.Coerce(value, typeof<obj>)
                    <@@ printfn "TODO write %A" %%o @@>)
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