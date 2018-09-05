module AvroLibraryTests

open Xunit
open Avro
open Avro.IO
open Avro.Generic
open System.IO
open Avro.File

let write schema items =
    let stream = new MemoryStream()
    let gdw = GenericDatumWriter<GenericRecord>(schema)
    let w = DataFileWriter.OpenWriter(gdw, stream)
    items |> Seq.iter w.Append
    w.Flush()
    stream.Position <- 0L
    stream
    

let read (stream: Stream) : list<GenericRecord> =
    let r = DataFileReader.OpenReader(stream)
    [ while r.HasNext() do yield! r.NextEntries ]

let rountrip schema items =
    items
    |> write schema
    |> read
   

[<Literal>]
let authorSchema = """
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type":{ "type": "array", "items": "string" } },
        {"name": "born", "type": "int"} ]
}
"""

[<Fact>]
let ``serialization of simple record rountrips`` () =
    let s = Schema.Parse authorSchema
    let item = GenericRecord(s :?> RecordSchema)
    item.Add("name", [|"joe"|])
    item.Add("born", 2000)
    let items = [item]
    let parsedItems = rountrip s items
    items = parsedItems
    |> Assert.True

type T = AvroProvider<authorSchema>

[<Fact>]
let ``serialization of simple type rountrips`` () =
    let s = Schema.Parse authorSchema
    let f = T.Factory()
    let item = f.author([|"joe"|], 2000)
    let items = [item :> GenericRecord]
    let parsedItems = rountrip s items
    items = parsedItems
    |> Assert.True




