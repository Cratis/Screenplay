---
title: HTTP API description
description: Export a Cratis Screenplay model as an OpenAPI 3.1 document describing each command and query with its route, method, payload, authorization and live updates.
---

## Describe the HTTP API of a model

A model's commands and queries become an HTTP API. The routes, methods and
payloads follow from the model, so a client, a test or a builder for another
stack can read them instead of copying a renderer's conventions. Export them
as an [OpenAPI 3.1](https://spec.openapis.org/oas/v3.1.0) document:

```bash
screenplay export path/to/screenplays --format openapi --output api.json
```

The document needs the executable model; a model that does not compile to one
is refused with its errors, as for `--format esm`. `--scope` keeps one module,
feature or slice. Over MCP, `read-workspace` with `view: "api-description"`
pages the same bytes.

## What the description holds

| Model | HTTP |
| --- | --- |
| A command | `POST` with the command's inputs as a JSON body, and `POST <route>/validate` to validate without running it |
| A query | `GET` with its `by` key and filters in the query string; a list query also takes `page`, `pageSize`, `sortBy` and `sortDirection` |
| An observable query | The same `GET`, marked `x-screenplay-observable`; open the route as a WebSocket to receive the result again on every change |
| `authorize` | `x-screenplay-authorization`: `anonymous: true` when the model declares none, otherwise the policy expression in `requires` and the policies it names |
| A policy | `x-screenplay-policies` at the root spells each policy's condition, such as `role "Librarian"` |

Every operation also carries `x-screenplay-kind` (`command`, `query` or
`validation`), `x-screenplay-id` (the semantic identity in the executable
model) and `x-screenplay-slice`. Concepts, types, read models and command
bodies are component schemas; an enumeration concept is an integer `enum`
with its value names in `x-enum-varnames`. Responses are wrapped in the
`CommandResult` (`response`) and `QueryResult` (`data`, `paging`) envelopes.
The document's `info.version` is the executable model's revision, and the
same model always gives the same bytes.

## Routes

A route is `/api/` followed by the kebab-cased module, feature and slice
names. A query route ends with the query name. A command route ends with the
command name only when its slice declares more than one command:

| Declaration | Route |
| --- | --- |
| Command `AddBook` alone in slice `Lending.Catalog.AddBook` | `POST /api/lending/catalog/add-book` |
| Command `RetireBook` beside another command in slice `ShelveBook` | `POST /api/lending/catalog/shelve-book/retire-book` |
| Query `AllBooks` in slice `CatalogList` | `GET /api/lending/catalog/catalog-list/all-books` |

These are the routes of an application rendered by the Cratis renderer. A
dash goes before every upper-case letter but the first, so `GetHTMLPage`
becomes `get-h-t-m-l-page`.

## Use it from .NET

`ApiDescription.For(model)` in `Cratis.Screenplay.Api` lists every operation
with its kind, route, method, parameters, authorization and, for a query, its
read model and whether it is live. `OpenApiDocument.Write(description)` writes
the document. Pass `new ApiConventions("service/v1", true)` to describe another
route prefix or to name every command in its route.

```csharp
using Cratis.Screenplay.Api;

var description = ApiDescription.For(model);
foreach (var operation in description.Operations)
{
    Console.WriteLine($"{operation.Method} {operation.Route} requires {operation.Authorization.Requires ?? "nothing"}");
}

File.WriteAllBytes("api.json", OpenApiDocument.Write(description));
```

To describe the screens that call these operations, use the
[UI contract](ui-contract.md).
