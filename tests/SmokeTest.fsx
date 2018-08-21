#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro
open Avro.Generic

type T = AvroProvider<Schema="""
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"},
        {"name": "foo", "type": ["null", "int"]}
    ]
}
""">

let a = T.author(name="Joe", born=1900, foo=System.Nullable())

printfn "name %s, born %d, foo %A" a.name a.born a.foo
