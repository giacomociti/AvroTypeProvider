#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro
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

type T = AvroProvider<Schema=schema>

let a = T.author(name="Joe",
                 born=1900,
                 word = T.Word [| 2uy; 3uy |],
                 inner = T.Inner("AA"),
                 innerOpt = T.Inner("A"),
                 innerMap = dict ["k", T.Inner("C")],
                 score = ResizeArray([1;2]),
                 scoreOpt = ResizeArray([System.Nullable(7)]),
                 suit = T.Suit.CLUBS,
                 codes = dict ["A",1; "B",3])

printfn "%A" (a.name, a.born, a.score, a.scoreOpt, a.codes)
printfn "%A" (a.word, a.suit, a.inner.id, a.innerOpt, a.innerMap)