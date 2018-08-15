namespace AvroTypeProvider

open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

[<TypeProvider>]
type TypeProvider (config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let ns = "Avro"
    let asm = Assembly.GetExecutingAssembly()
    let providedAssembly = ProvidedAssembly()

    let createType (typeName, schema) =
        let enclosingType =
            ProvidedTypeDefinition(
                assembly = providedAssembly,
                namespaceName = ns,
                className = typeName,
                baseType = Some typeof<obj>,
                isErased = false,
                hideObjectMethods = true)

        providedAssembly.AddTypes [enclosingType]
        enclosingType
    
    do
        let avroProvider =
            ProvidedTypeDefinition(
                assembly = asm,
                namespaceName = ns,
                className = "AvroProvider",
                baseType = Some typeof<obj>,
                isErased = false)

        let parameters = [ProvidedStaticParameter("Schema", typeof<string>)]
        avroProvider.DefineStaticParameters(parameters, (fun typeName args ->
            createType(typeName, args.[0] :?> string)))

        this.AddNamespace(ns, [avroProvider])

[<assembly:TypeProviderAssembly>]
do ()