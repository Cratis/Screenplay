---
title: "Domain"
description: "Declare the business domain a Screenplay .play file belongs to, as a qualified name that groups related files."
---

When a library has cataloging, lending, and membership features, their names need a business context. `domain Library` records that context in the model. It is a qualified name that groups related files, not an instruction to create a database, tenant, service, or deployment.

A domain line is optional. Use it to name the business area before describing the behavior inside it; [getting started](getting-started.md) uses `Library` for that purpose.

## Syntax

```screenplay
domain <Qualified.Name>
```

## Rules

- **Optional** — a file without a `domain` line belongs to no domain.
- **At most one per file** — a second `domain` line is a compile error.
- **First in the file** — the declaration must appear before everything else, including imports.

## Example

```screenplay
domain Sales

import Customers.CustomerRegistered

module Invoicing
  ...
```

This outline places the `Invoicing` module in the `Sales` business domain; the ellipsis stands for its features and slices. An `import` with an unquoted, qualified name names a contract from another bounded context. To bring in other `.play` files of the same application, quote a path or glob instead — see [Imports](imports.md).

## Domain, module, and application

These boundaries answer different questions:

- **Domain:** what business area does this file describe?
- **Module:** which namespace groups these features and slices? A module commonly represents a bounded context.
- **File:** where is this part of the authored model stored? A file need not contain the whole system.
- **Application:** which files are compiled together? A folder is one application; a root file includes itself and its quoted imports, not every sibling automatically.

Giving two files the same domain does not import one into the other or configure runtime isolation. See [Folders](folders.md) for compilation boundaries and [Environments](environments.md) for values supplied when the model runs.

Within that business context, [concepts](concepts.md) name the values your rules use. `BookId : Uuid` distinguishes a book identifier from other UUIDs; it does not choose a storage engine or provision an identity service.
