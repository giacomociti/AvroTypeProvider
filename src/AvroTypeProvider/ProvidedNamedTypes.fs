namespace AvroTypeProvider

#nowarn "25" // for pattern matching Expr[]

open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Avro
open AvroTypes

module ProvidedNamedTypes =

    let private addDefaultCtor (providedType: ProvidedTypeDefinition) =
        ProvidedConstructor([], invokeCode = fun _ -> <@@ () @@>)
        |> providedType.AddMember

    let private addFieldWithProperties
        (providedType: ProvidedTypeDefinition)
        fieldName
        fieldType =
        let providedField = ProvidedField("_" + fieldName, fieldType)
        providedType.AddMember providedField

        let providedProperty =
            ProvidedProperty(fieldName, fieldType,
                getterCode = (fun [record] ->
                    Expr.FieldGet(record, providedField)),
                setterCode = (fun [record; value] ->
                    Expr.FieldSet(record, providedField, value)))
        providedType.AddMember providedProperty

    let setRecord types fields providedType =
        addDefaultCtor providedType
        for f: Field in fields do
            let t = getType types f.Schema
            addFieldWithProperties providedType f.Name t


    let setFixed size providedType = () //TODO

    let setEnum symbols providedType = () //TODO
