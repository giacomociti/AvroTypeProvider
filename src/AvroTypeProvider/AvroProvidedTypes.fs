namespace AvroTypeProvider

open System.Collections.Generic
open ProviderImplementation.ProvidedTypes
open Avro
open AvroTypes
open ProvidedNamedTypes

module AvroProvidedTypes =

    let private providedType assembly (schema: NamedSchema) =
        let baseType =
            match schema with
            | Enum _ -> typeof<Enum>
            | _ -> typeof<Record>
        ProvidedTypeDefinition(
            assembly = assembly,
            namespaceName = schema.Namespace,
            className = schema.Name,
            baseType = Some baseType,
            isErased = true,
            hideObjectMethods = true)


    let private namedSchemas schema =
        let schemas = Dictionary()

        let previouslyCollected =
            function
            | Named n ->
                if schemas.ContainsKey n.SchemaName
                then true
                else schemas.Add(n.SchemaName, n)
                     false
            | _ -> false

        let rec collectNamedSchemas s =
            if not (previouslyCollected s) then
                match s with
                | Primitive _ -> ()
                | Union schema -> schema.Schemas |> Seq.iter collectNamedSchemas
                | Array schema -> collectNamedSchemas schema.ItemSchema
                | Map schema -> collectNamedSchemas schema.ValueSchema
                | Named schema ->
                    match schema with
                    | Record schema ->
                        schema.Fields
                        |> Seq.iter (fun f -> collectNamedSchemas f.Schema)
                    | _ -> ()

        collectNamedSchemas schema
        schemas :> IReadOnlyDictionary<_,_>

    let private namedTypes assembly (schemas: IReadOnlyDictionary<_,_>) =
        let types = Dictionary()
        for s in schemas do types.Add(s.Key, providedType assembly s.Value)
        types :> IReadOnlyDictionary<_,_>

    let setProvidedType types providedType =
        function
        | Record schema -> setRecord types schema providedType
        | Enum schema -> setEnum schema.Symbols providedType
        | Fixed schema -> setFixed schema.Size providedType

    let addProvidedTypes (enclosingType: ProvidedTypeDefinition) schema =
        let assembly = enclosingType.Assembly
        let schemas = namedSchemas schema
        let types = namedTypes assembly schemas
        for t in types do setProvidedType types t.Value schemas.[t.Key]
        for t in types do enclosingType.AddMember t.Value