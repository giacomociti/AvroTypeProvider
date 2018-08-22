namespace AvroTypeProvider

#nowarn "25" // for pattern matching Expr[]

open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Avro
open AvroTypes
open Avro.Generic

module ProvidedNamedTypes =

    let setRecord types (schema:RecordSchema) (providedType: ProvidedTypeDefinition) =

        let getProperty (fieldName, fieldType) =
            ProvidedProperty(fieldName, fieldType,
                getterCode = (fun [record] ->
                    <@@ (%%record: Record).GenericRecord.Item fieldName @@>))

        let getParameter (fieldName, fieldType) =
            ProvidedParameter(fieldName, fieldType)         


        let fields =
            schema.Fields 
            |> Seq.map (fun f -> f.Name, getType types f.Schema)
            |> Seq.toList
        let fieldNames = fields |> List.map fst
        let properties = fields |> List.map getProperty
        let parameters = fields |> List.map getParameter        

        let schemaText = schema.ToString()
        
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

    let setFixed (schema: FixedSchema) (providedType: ProvidedTypeDefinition) =
        let schemaText = schema.ToString()
        let parameter = ProvidedParameter("bytes", typeof<byte[]>)
        let ctor = ProvidedConstructor([parameter], invokeCode = fun [par] ->
          ( let s = <@@ (Schema.Parse schemaText) :?> FixedSchema @@>
            <@@ Runtime.CreateFixed(%%s, %%par) @@> ))
        providedType.AddMember ctor

    let setEnum (schema: EnumSchema) (providedType: ProvidedTypeDefinition) =
        let schemaText = schema.ToString()
        let s = <@@ (Schema.Parse schemaText) :?> EnumSchema @@>
        let properties =
            schema.Symbols
            |> Seq.map (fun symbol ->
                ProvidedProperty(symbol, providedType,
                    getterCode = (fun _ ->
                        <@@ Runtime.CreateEnum(%%s, symbol) @@>),
                    isStatic = true))
            |> Seq.toList
        providedType.AddMembers properties