namespace AvroTypeProvider

#nowarn "25" // for pattern matching Expr[]

open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Avro
open AvroTypes
open Avro.Generic

module ProvidedNamedTypes =

    let private getProperty (fieldName, fieldType) =
        ProvidedProperty(fieldName, fieldType,
            getterCode = (fun [record] ->
                <@@ (%%record: Record).GenericRecord.Item fieldName @@>))

    let private getParameter (fieldName, fieldType) =
        ProvidedParameter(fieldName, fieldType)         

    let setRecord types (schema:RecordSchema) (providedType: ProvidedTypeDefinition) =
        let fields =
            schema.Fields 
            |> Seq.map (fun f -> f.Name, getType types f.Schema)
            |> Seq.toList
        let fieldNames = fields |> List.map fst
        let properties = fields |> List.map getProperty
        let parameters = fields |> List.map getParameter        

        let schemaText = schema.ToString()

        let setValue (r: Expr) fieldName fieldType fieldValue =
            let v = Expr.Coerce(fieldValue, fieldType)
            <@@ (%%r:GenericRecord).Add(fieldName, v :> obj) @@>
        
        let ctor = ProvidedConstructor(parameters, invokeCode = fun pars ->
          ( let s = <@@ (Schema.Parse schemaText) :?> RecordSchema @@>
            let values =
              [ for name, par in List.zip fieldNames pars do
                let v = Expr.Coerce(par, typeof<obj>)
                yield Expr.NewTuple [Expr.Value name; v]
              ]
            let array = Expr.NewArray(typeof<string*obj>, values)
            <@@ Runtime.CreateRecord(%%s, %%array) @@> ))
            
        providedType.AddMember ctor
        providedType.AddMembers properties

        

    let setFixed size providedType = () //TODO

    let setEnum symbols (providedType: ProvidedTypeDefinition) =
        providedType.SetEnumUnderlyingType typeof<int>
        symbols
        |> Seq.mapi (fun i s -> ProvidedField.Literal(s, providedType, i))
        |> Seq.iter providedType.AddMember
