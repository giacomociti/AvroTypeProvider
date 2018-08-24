#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro
open Avro.IO
open Avro.File
open Avro.Generic
open System.Collections.Generic

[<Literal>]
let schema = """
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"},
        {"name": "word", "type": {"type": "fixed", "size": 2, "name": "Word"}},
        {"name": "inner", "type":
            {"type": "record",
             "name": "Inner",
             "fields": [{"name": "id", "type": "string"}]}},
        {"name": "innerOpt", "type": ["null", "Inner"]},
        {"name": "innerMap", "type": {"type": "map", "values": "Inner"}},
        {"name": "score", "type": {"type": "array", "items": "int"}},
        {"name": "scoreOpt", "type": {"type": "array", "items": ["null", "int"]}},
        {"name": "suit", "type":
            { "type": "enum",
              "name": "Suit",
              "symbols" : ["SPADES", "HEARTS", "DIAMONDS", "CLUBS"] }
	    },
        {"name": "codes", "type": {"type": "map", "values": "int"}}
    ]
}
"""


let s = Schema.Parse schema

type T = AvroProvider<schema>

let f = T.Factory()


let toDict items =
    let result = Dictionary()
    for (k, v) in items do result.Add(k, v)
    result

let d = toDict ["k", f.Inner("C")]
let d' = toDict ["A",1; "B",3]

let a = f.author(name="Joe",
                 born=1900,
                 word = f.Word [| 2uy; 3uy |],
                 inner = f.Inner("AA"),
                 innerOpt = f.Inner("A"),
                 innerMap =  d,
                 score = ([1;2] |> List.toArray),
                 scoreOpt = ([System.Nullable(7)] |> List.toArray),
                 suit = f.Suit.CLUBS,
                 codes = d')

printfn "%A" (a.name, a.born, a.score, a.scoreOpt, a.codes)
printfn "%A" (a.word.Value, a.suit.Value, a.inner.id, a.innerOpt, a.innerMap)

let path = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "temp", "test.avro")
T.Write (path, [a])
let a' = T.Read(path) |> Seq.exactlyOne
a = a' // still false?
