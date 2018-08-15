namespace AvroTypeProvider

open Avro

module AvroTypes =

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