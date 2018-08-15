#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"
#r "AvroTypeProvider.dll"

open Avro

[<Literal>]
let schema = """
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"},
    ]
}
"""

type T = AvroProvider<Schema=schema>