namespace AvroTypeProvider

open System.Collections.Generic
open SchemaParsing

module AvroTypes =

    let arrayType t =
        typedefof<IList<_>>.MakeGenericType [| t |]

    let mapType t =
        typedefof<IDictionary<_, _>>.MakeGenericType [| typeof<string>; t |]

    let nullableType t =
        typedefof<System.Nullable<_>>.MakeGenericType [| t |]

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