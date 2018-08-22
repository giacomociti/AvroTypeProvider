namespace AvroTypeProvider

open Avro.Generic

type Record = 
    { GenericRecord: GenericRecord }

    static member Create(genericRecord) =
        { GenericRecord = genericRecord }

type Fixed =
    { GenericFixed: GenericFixed }

    member this.Value = this.GenericFixed.Value

    static member Create(genericFixed) =
        { GenericFixed = genericFixed }
    
type Enum = 
    { GenericEnum: GenericEnum }

    member this.Value = this.GenericEnum.Value

    static member Create(genericEnum) =
        { GenericEnum = genericEnum }

type Runtime =
    static member CreateRecord(schema, values: array<string*obj>) =
        let record = GenericRecord schema
        Array.iter record.Add values
        Record.Create record

    static member CreateFixed(schema, value) =
        Fixed.Create (GenericFixed(schema, value))

    static member CreateEnum(schema, value) =
        Enum.Create (GenericEnum(schema, value))