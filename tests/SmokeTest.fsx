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
        {"name": "score", "type": {"type": "array", "items": "int"}},
        {"name": "scoreOpt", "type": {"type": "array", "items": ["null", "int"]}},
        {"name": "codes", "type": {"type": "map", "values": "int"}},
        {"name": "foo", "type": ["null", "int"]}
    ]
}
"""

Schema.Parse schema

type T = AvroProvider<Schema=schema>

let score = ResizeArray([1;2])
let scoreOpt = ResizeArray([System.Nullable(7)])
let codes = dict ["A",1; "B",3]
let a = T.author(name="Joe", 
                 born=1900, 
                 score=score,
                 scoreOpt = scoreOpt,
                 codes = codes,
                 foo=System.Nullable())

printfn "%A" (a.name, a.born, a.score, a.scoreOpt, a.codes, a.foo)
