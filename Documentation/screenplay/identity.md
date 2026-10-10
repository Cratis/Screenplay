---
title: Identity details
description: Declare typed caller details from claims, queries or opaque implementations without changing token built-ins.
---

A token identifies the caller, but it may not carry everything your application knows about that person.
The top-level `identity` block describes those additional **identity details**, with a type and exactly one
source per detail. It is separate from [authentication providers](authentication.md).

**The block is authoring metadata only.** It parses, validates, prints and survives AST editing, but adds
nothing to the executable semantic model (ESM). Reading a declared detail in an executable clause reports
`PLAY0268` until [executable admission (#600)](https://github.com/Cratis/Screenplay/issues/600). A valid
declaration is not evidence that a runtime resolves its sources.

## Declare the details

````screenplay
identity
  description "What the application knows about whoever is calling"
  department String optional from claim "department"
  organization Organization optional from query MyOrganization by $identity.id
  isPartner Bool
    ```csharp
    return context.Identity.HasRole("Partner");
    ```
  externalReference String
    file Identity/ExternalReference.cs

module Organizations
  feature Membership
    slice StateView Mine
      readmodel Organization
        name String
      query MyOrganization => Organization optional
        by userId String
````

There is at most one block per document and per assembled folder. An optional `description` accepts the
same quoted or fenced-text forms as other declarations. Detail lines use ordinary type references:
collections (`[]`) and optional values (`optional`, or the accepted `?` spelling).

| Source | Form | Contract |
| --- | --- | --- |
| Claim | `department String from claim "department"` | The claim name is opaque. |
| Query | `organization Organization optional from query MyOrganization by $identity.id` | The query must exist, declare a key and return one result, not a list. |
| Inline code | A typed detail followed by an indented tagged code fence | The source is opaque; Screenplay does not compile or execute it. The legacy language-line/fence form is accepted with its existing warning. |
| File | A typed detail followed by an indented `file` directive | The same repository-relative [file reference](file-references.md) convention as handlers and rules. |

A detail without a source is an error. A detail cannot combine sources. Claim and query sources have no
body; refresh and cache directives are errors because those decisions belong to the runtime.

## Query resolution rules

A query source requires `by <expression>`. The key may read token built-ins through `$identity` or
`$context.identity`, including opaque claim names, or use a literal. It cannot read another detail, command
input, a read-model path or an environment value. The query's authorization policies, including inherited
module and feature gates, cannot depend on additional identity details either.

The query result type must match the detail's declared type. An optional result needs an optional detail;
a required result may populate an optional detail. Unknown detail types report the ordinary unknown-type
warning. Unknown or ambiguous queries, unkeyed/list queries and incompatible types are errors.

## Caller paths and names

`$identity.<detail>` recognizes names from the assembled block, including declarations later in the
source or in another file. `$context.identity` remains **built-ins only**. The built-ins are `id`, `name`,
`userName`, `isAuthenticated`, `roles` and `claims`; a detail cannot redeclare them. Detail names must be
unique. Undeclared `$identity` properties keep the `PLAY0155` warning.

a `scoped to` scope is an opaque name, not a reference to an identity detail

## Tooling and compatibility

`ApplicationSyntax.Identity` is an additive init-only property. The walker visits the block, each detail,
its type and its source. Syntax JSON and `syntax-schema` expose the same nodes. Folder expansion writes
the block to `application.play`. MCP declaration views list `Identity` and `IdentityDetail`; query-source
references participate in reference navigation. Detail renames have no dedicated logical rename operation:
use a reviewed AST edit and update the caller paths explicitly. There are no automatic repairs for these
identity diagnostics.

The ESM, the reference evaluator and the `Contexts.Identity` runtime contract are unchanged. See
[contexts](context.md) for the caller built-ins that are already available to code and declarative mappings,
and [diagnostics](diagnostics.md) for identity source errors.
