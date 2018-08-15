#I "../src/AvroTypeProvider/bin/Debug/netstandard2.0"

#r "libs/confluent.apache.avro.1.7.7.5.nupkg_FILES/lib/netstandard2.0/Confluent.Apache.Avro.dll"
#r "libs/newtonsoft.json.11.0.2.nupkg_FILES/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "AvroTypeProvider.dll"

open Avro
open Avro.File

let authorFile = "tests/temp/author.avro"

let serialize writer (avroFilePath: string) items =
    let codec = Codec.CreateCodec(Codec.Type.Deflate)
    use fileWriter = DataFileWriter.OpenWriter(writer, avroFilePath, codec)
    items |> List.iter fileWriter.Append

[<Literal>]
let schema = """
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"},
        {"name": "suit", "type" :
            { "type": "enum",
              "name": "Suit",
              "symbols" : ["SPADES", "HEARTS", "DIAMONDS", "CLUBS"]
            }
        }
    ]
}
"""

type T = AvroProvider<Schema=schema>

let a = T.author()
a.name <- "AAA"
a.born <- 123
a.suit <- T.Suit.CLUBS

let w = T.authorDatumWriter()
serialize w authorFile [a]