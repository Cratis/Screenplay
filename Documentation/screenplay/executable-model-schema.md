---
title: Executable model JSON Schema
description: Download the JSON Schema for each released version of the Screenplay executable model, then validate ESM documents or generate types for them in any language.
---

## Read ESM documents outside .NET

The compiler turns a model into the canonical executable semantic model (ESM): one
JSON document whose shape is frozen per `schemaVersion`. To read, validate or
generate types for those documents in Node, Java, Python, Go or any other stack,
use the JSON Schema published for the version the document declares.

Each schema is a [JSON Schema draft 2020-12](https://json-schema.org/draft/2020-12)
document at a stable address:

```text
https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/esm-v<major>.schema.json
https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/esm-v<major>.<minor>.schema.json
```

A `.0` version uses the first form and a released minor the second; the `semanticVersion`
member of a document names the version. For example, a document with
`"semanticVersion": "41.0"` validates against
[`esm-v41.schema.json`](https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/esm-v41.schema.json),
and one with `"semanticVersion": "37.1"` against
[`esm-v37.1.schema.json`](https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/esm-v37.1.schema.json).
Each schema's `$id` is its download address. Schemas are published for every
released version from 1.0 on; version 39 was never released.

## Validate a document

This Node example validates a document with [Ajv](https://ajv.js.org):

```javascript
import Ajv2020 from "ajv/dist/2020.js";
import { readFile } from "node:fs/promises";

const document = JSON.parse(await readFile("model.esm.json", "utf8"));
const [major, minor] = document.semanticVersion.split(".");
const name = minor === "0" ? `esm-v${major}.schema.json` : `esm-v${major}.${minor}.schema.json`;
const schema = await (await fetch(`https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/${name}`)).json();

const validate = new Ajv2020({ allowUnionTypes: true }).compile(schema);
console.log(validate(document) ? "valid" : validate.errors);
```

The schemas use type unions such as `["string", "null"]`, so Ajv's strict mode needs
`allowUnionTypes`.

## What the schema describes

A schema names every member a version writes, how objects nest, which members hold
arrays, and the JSON type of every value. Every object refuses members the version
does not write, so a document from another version fails rather than being read
partially. The envelope members `schema`, `schemaVersion`, `languageVersion`,
`semanticVersion`, `revision` and `application` are required, and `schemaVersion`
and `semanticVersion` must equal the schema's version.

A schema checks shape, not meaning. It does not check identifier formats, the
semantic revision, references between declarations or any rule the compiler
enforces. A document that validates is well-formed; only the Screenplay reader
admits it. Canonical documents omit members that have no value, so the schema does
not require members inside the application.

## How the schemas are kept correct

The schemas are generated from the frozen ESM vocabulary the strict reader uses, so
they cannot describe a member the reader does not know. The build fails when:

- a published schema differs from what the generator produces for its version,
- a released version has no published schema, or a schema exists for a version that was not released, or
- any canonical ESM document in the repository, the full goldens and the
  canonical corpus included, does not validate against its version's schema.

In .NET, `EsmJsonSchema` in `Cratis.Screenplay.Semantics.Serialization` generates the
same text: `EsmJsonSchema.Versions` lists the published versions,
`EsmJsonSchema.FileName(version)` names the file and `EsmJsonSchema.For(version)`
returns the schema.

To see which slices changed between two versions of a model, rather than the whole
document, use the [per-slice change summary](model-comparison.md#group-changes-by-slice).
