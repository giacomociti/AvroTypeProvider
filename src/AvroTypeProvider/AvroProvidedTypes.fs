namespace AvroTypeProvider

open System.Collections.Generic
open Microsoft.FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Avro
open Avro.Generic
open AvroTypes
open SchemaParsing

#nowarn "25" // for pattern matching Expr[]

module AvroProvidedTypes =

    let private typeContainer assembly =
        ProvidedTypeDefinition(
            assembly = assembly,
            namespaceName = "",
            className = "Types",
            baseType = Some typeof<obj>,
            isErased = true,
            hideObjectMethods = true)

    let createFixed (factory: ProvidedTypeDefinition) (schema: FixedSchema) (providedType: ProvidedTypeDefinition) =
        let schemaName = schema.Fullname
        ProvidedMethod(
            methodName = providedType.Name,
            parameters = [ ProvidedParameter("bytes", typeof<byte[]>) ],
            returnType = providedType,
            invokeCode = fun [this; value] ->
                <@@ (%%this :> Factory).CreateFixed(schemaName, %%value) @@>)
        |> factory.AddMember

    let createEnum (factory: ProvidedTypeDefinition) (schema: EnumSchema) (providedType: ProvidedTypeDefinition) =
        let enumFactory =
            ProvidedTypeDefinition(
                assembly = providedType.Assembly,
                namespaceName = "",
                className = providedType.Name + "Factory",
                baseType = Some typeof<Factory>,
                isErased = true,
                hideObjectMethods = true)
        let schemaName = schema.Fullname
        schema.Symbols
        |> Seq.map (fun symbol ->
            ProvidedProperty(symbol, providedType,
                getterCode = (fun [this] ->
                    <@@ (%%this :> Factory).CreateEnum(schemaName, symbol) @@>)))
        |> Seq.toList
        |> Seq.iter enumFactory.AddMember

        enumFactory
        |> factory.AddMember

        ProvidedProperty(providedType.Name, enumFactory,
            getterCode = (fun [this] -> <@@ (%%this :> Factory) @@>))
        |> factory.AddMember

    let createRecord types (factory: ProvidedTypeDefinition) (schema:RecordSchema) (providedType: ProvidedTypeDefinition) =

        let getParameter (fieldName, fieldType) =
            ProvidedParameter(fieldName, fieldType)

        let fields =
            schema.Fields
            |> Seq.map (fun f -> f.Name, getType types f.Schema)
            |> Seq.toList
        let fieldNames = fields |> List.map fst
        let schemaName = schema.Fullname
        ProvidedMethod(
            methodName = providedType.Name,
            parameters = (fields |> List.map getParameter),
            returnType = providedType,
            invokeCode = fun (this::pars) ->
              ( let values =
                  [ for name, par in List.zip fieldNames pars do
                    let v = Expr.Coerce(par, typeof<obj>)
                    // TODO adjust array, map and nullable types?
                    yield Expr.NewTuple [Expr.Value name; v]
                  ]
                let array = Expr.NewArray(typeof<string*obj>, values)
                <@@ (%%this :> Factory).CreateRecord(schemaName, %%array) @@> ))
        |> factory.AddMember

    let private typeFactory assembly (schema: RecordSchema) =
        let factory =
            ProvidedTypeDefinition(
                assembly = assembly,
                namespaceName = "",
                className = "Factory",
                baseType = Some typeof<Factory>,
                isErased = true,
                hideObjectMethods = true)
        let schemaText = schema.ToString()
        let ctor = ProvidedConstructor([], invokeCode = fun _ ->
            <@@ Factory(schemaText) @@>)
        factory.AddMember ctor
        factory

    let setRecord types (schema:RecordSchema) (providedType: ProvidedTypeDefinition) =

        let getProperty (fieldName, fieldType) =
            ProvidedProperty(fieldName, fieldType,
                getterCode = (fun [record] ->
                    <@@ (%%record: GenericRecord).Item fieldName @@>))

        schema.Fields
        |> Seq.map (fun f -> getProperty(f.Name, getType types f.Schema))
        |> Seq.toList
        |> providedType.AddMembers


    let private providedType assembly (schema: NamedSchema) =
        let baseType =
            match schema with
            | Record _ -> typeof<GenericRecord>
            | Fixed _ -> typeof<GenericFixed>
            | Enum _ -> typeof<GenericEnum>
        ProvidedTypeDefinition(
            assembly = assembly,
            namespaceName = schema.Namespace,
            className = schema.Name,
            baseType = Some baseType,
            isErased = true,
            hideObjectMethods = true)

    let private namedTypes assembly (schemas: IReadOnlyDictionary<_,_>) =
        let types = Dictionary()
        for s in schemas do types.Add(s.Key, providedType assembly s.Value)
        types :> IReadOnlyDictionary<_,_>

    let setProvidedType types providedType =
        function
        | Record schema -> setRecord types schema providedType
        | Enum _
        | Fixed _ -> ()

    let addFactoryMethod types (factory: ProvidedTypeDefinition) providedType =
        function
        | Record schema -> createRecord types factory schema providedType
        | Enum schema -> createEnum factory schema providedType
        | Fixed schema -> createFixed factory schema providedType

    let addProvidedTypes (enclosingType: ProvidedTypeDefinition) schema =
        let assembly = enclosingType.Assembly
        let schemas = namedSchemas schema
        let types = namedTypes assembly schemas
        for t in types do setProvidedType types t.Value schemas.[t.Key]
        let factory = typeFactory assembly schema
        enclosingType.AddMember factory
        for t in types do addFactoryMethod types factory t.Value schemas.[t.Key]
        let ts = typeContainer assembly
        for t in types do ts.AddMember t.Value
        enclosingType.AddMember ts