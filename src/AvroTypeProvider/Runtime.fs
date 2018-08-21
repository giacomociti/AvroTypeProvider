namespace AvroTypeProvider

open Avro.Generic

type Record = 
    { GenericRecord: GenericRecord }

    static member Create(genericRecord) =
        { GenericRecord = genericRecord }


    
type Enum = {
    GenericEnum: GenericEnum
}

type Runtime =
    static member CreateRecord(schema, values: array<string*obj>) =
        let record = GenericRecord schema
        Array.iter record.Add values
        Record.Create record