#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro
open Avro.IO
open Avro.File
open Avro.Generic

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


Schema.Parse schema

type X = AvroProvider<Schema=schema>
type T = X.Types
let f = X.Factory()

let a = f.author(name="Joe",
                 born=1900,
                 word = f.Word [| 2uy; 3uy |],
                 inner = f.Inner("AA"),
                 innerOpt = f.Inner("A"),
                 innerMap = dict ["k", f.Inner("C")],
                 score = ResizeArray([1;2]),
                 scoreOpt = ResizeArray([System.Nullable(7)]),
                 suit = f.Suit.CLUBS,
                 codes = dict ["A",1; "B",3])

printfn "%A" (a.name, a.born, a.score, a.scoreOpt, a.codes)
printfn "%A" (a.word.Value, a.suit.Value, a.inner.id, a.innerOpt, a.innerMap)


