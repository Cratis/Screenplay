# Domain

A `.play` file can declare the domain it belongs to. The domain is a qualified name that groups related files — typically the name of the business domain a bounded context sits in.

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
