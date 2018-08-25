module Tests

open Xunit
open Avro

type T = AvroProvider<"""
{
    "type": "record",
    "name": "author",
    "fields": [
        {"name": "name", "type": "string"},
        {"name": "born", "type": "int"} ]
}
""">


[<Fact>]
let ``serialization of simple record rountrips`` () =
    let f = T.Factory()
    let a = f.author(name="Joe", born = 2000)
    T.Write("test.avro", [a])
    let a' = T.Read("test.avro") |> Seq.exactlyOne
    Assert.Equal(a.name, a'.name)
    Assert.Equal(a.born, a'.born)









