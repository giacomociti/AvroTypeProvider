# AvroTypeProvider

This is an attempt to create a type provider for [Avro](http://avro.apache.org/).

The type provider is erased (an early attempt to make it generative was discarded)
and based on the C# port of the [Apache library maintained by Confluence](https://github.com/confluentinc/avro).

At the moment this project is mainly a personal experiment, with no commitment to turn it into something usable.

## TODO list

- tests
- docs
- automated build and release
- proper handling of reference to the external Avro library
- better data types for array, map and optional fields
- path resolution for file parameter
- improve names and namespace handling
- improve support for union types


