namespace AvroTypeProvider

open System.Collections.Generic
open ProviderImplementation.ProvidedTypes
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

    let (| NullOrType | TypeOrNull | RealUnion |) (schema: UnionSchema) =
        let schemas = schema.Schemas |> Seq.toList
        match schemas with
        | [s1; s2] when s1.Tag = Tag.Null -> NullOrType s2
        | [s1; s2] when s2.Tag = Tag.Null -> TypeOrNull s1
        | _ -> RealUnion schemas

    let arrayType t =
        typedefof<IList<_>>

            //.GetGenericTypeDefinition()
            .MakeGenericType [| t |]

    let mapType t =
        typedefof<IDictionary<_, _>>
            //.GetGenericTypeDefinition()
            .MakeGenericType [| typeof<string>; t |]

    let nullableType t =
        typedefof<System.Nullable<_>>
            //.GetGenericTypeDefinition()
            .MakeGenericType [| t |]

    let rec getType (types: IReadOnlyDictionary<_,_>) =
        function
        | Primitive schema ->
            match schema.Tag with
            | Tag.Null -> typeof<obj> // void?
            | Tag.Int -> typeof<int>
            | Tag.Long -> typeof<int64>
            | Tag.Float -> typeof<float>
            | Tag.Double -> typeof<double>
            | Tag.Bytes -> typeof<byte[]>
            | Tag.Boolean -> typeof<bool>
            | Tag.String -> typeof<string>
            | _ -> failwithf "Unknown schema type: %A" schema.Tag
        | Named schema -> types.[schema.SchemaName] :> System.Type
        | Union schema ->
            match schema with
            | NullOrType s
            | TypeOrNull s ->
                let t = getType types s
                if t.IsValueType then nullableType t else t
            | RealUnion _ -> typeof<obj> // fallback
        | Array schema -> arrayType (getType types schema.ItemSchema)
        | Map schema -> mapType (getType types schema.ValueSchema)