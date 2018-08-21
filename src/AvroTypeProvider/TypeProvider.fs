namespace AvroTypeProvider

open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Avro
open AvroProvidedTypes

[<TypeProvider>]
type TypeProvider (config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let ns = "Avro"
    let asm = Assembly.GetExecutingAssembly()
    //let providedAssembly = ProvidedAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<Record>.Assembly.GetName().Name = asm.GetName().Name) 

    let createType (typeName, schema) =
        let json =
            if System.IO.File.Exists schema
            then System.IO.File.ReadAllText schema
            else schema
        let avroSchema = Schema.Parse json :?> NamedSchema
        let enclosingType =
            ProvidedTypeDefinition(
                assembly = asm,
                namespaceName = ns,
                className = typeName,
                baseType = Some typeof<obj>,
                isErased = true,
                hideObjectMethods = true)
        addProvidedTypes enclosingType avroSchema
        enclosingType

    do
        let avroProvider =
            ProvidedTypeDefinition(
                assembly = asm,
                namespaceName = ns,
                className = "AvroProvider",
                baseType = Some typeof<obj>,
                isErased = true)

        let parameters = [ProvidedStaticParameter("Schema", typeof<string>)]
        avroProvider.DefineStaticParameters(parameters, (fun typeName args ->
            createType(typeName, args.[0] :?> string)))

        this.AddNamespace(ns, [avroProvider])

[<assembly:TypeProviderAssembly>]
do ()