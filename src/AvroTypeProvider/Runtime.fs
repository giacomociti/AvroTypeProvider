namespace AvroTypeProvider

open Avro
open Avro.Generic
open System.Collections.Generic

module SchemaParsing =

    type Tag = Avro.Schema.Type
    let (| Primitive | Named | Union | Array | Map |) (schema: Schema) =
        match schema with
        | :? PrimitiveSchema as s -> Primitive s
        | :? NamedSchema as s -> Named s
        | :? UnionSchema as s -> Union s
        | :? ArraySchema as s -> Array s
        | :? MapSchema as s -> Map s
        | _ -> failwithf "Unknown schema type: %A" schema.Tag
    let (| Record | Enum | Fixed |) (schema: NamedSchema) =
        match schema.Tag with
        | Tag.Record -> Record (schema :?> RecordSchema)
        | Tag.Enumeration -> Enum (schema :?> EnumSchema)
        | Tag.Fixed -> Fixed (schema :?> FixedSchema)
        | _ -> failwithf "Unknown schema type: %A" schema.Tag
    let (| NullOrType | TypeOrNull | RealUnion |) (schema: UnionSchema) =
        let schemas = schema.Schemas |> Seq.toList
        match schemas with
        | [s1; s2] when s1.Tag = Tag.Null -> NullOrType s2
        | [s1; s2] when s2.Tag = Tag.Null -> TypeOrNull s1
        | _ -> RealUnion schemas

    let namedSchemas schema =
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




open SchemaParsing
open System.ComponentModel

type Factory(schemaText) =
    let schemas = namedSchemas (Schema.Parse schemaText)

    let filter tag =
        schemas
        |> Seq.filter (fun x -> x.Value.Tag = tag)
        |> Seq.map (fun x -> x.Key.Fullname, x.Value :?> 'a)
        |> dict

    let recordSchemas: IDictionary<_, RecordSchema> = filter Tag.Record
    let enumSchemas: IDictionary<_, EnumSchema> = filter Tag.Enumeration
    let fixedSchemas: IDictionary<_, FixedSchema> = filter Tag.Fixed

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    member __.CreateFixed(fullName, value) =
        GenericFixed(fixedSchemas.[fullName], value)

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    member __.CreateEnum(fullName, value) =
        GenericEnum(enumSchemas.[fullName], value)

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    member __.CreateRecord(fullName, values: array<string*obj>) =
        let genericRecord = GenericRecord (recordSchemas.[fullName])
        Array.iter genericRecord.Add values
        genericRecord
