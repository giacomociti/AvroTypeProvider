#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro

type T = AvroProvider<Schema="""
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"},
        {"name": "foo", "type": ["null", "int"]},
        {"name": "foos", "type":
            { "type": "array",
              "items": "string"
            }
        },
        {"name": "fooDict", "type":
            { "type": "map",
              "values": "int"
            }
        }

    ]
}
""">

open Avro.File
open Avro.Generic

let authorFile = __SOURCE_DIRECTORY__ + "/temp/author.avro"

let serialize writer (avroFilePath: string) items =
    let codec = Codec.CreateCodec(Codec.Type.Deflate)
    use fileWriter = DataFileWriter.OpenWriter(writer, avroFilePath, codec)
    items |> List.iter fileWriter.Append

let deserialize (avroFilePath: string) = [

    use fileReader = DataFileReader<GenericRecord>.OpenReader(avroFilePath)
    for item in fileReader.NextEntries do yield item
]


let a = T.author()
a.name <- "AAA"
a.born <- 123
//a.suit <- T.Suit.CLUBS
a.foo <- System.Nullable(3)
a.foos <- ResizeArray(["aa"; "bb"])
a.fooDict <- dict ["a",11; "b",22]
let w = T.authorDatumWriter()
serialize w authorFile [a]

let items = deserialize authorFile

items.[0].["foos"]
items.[0].["fooDict"]

