---
title: "Screenplay domain declarations"
description: "Use the optional domain declaration to place a Screenplay .play file in a business domain by qualified name, first in the file, once per file."
---

# Domain

Use `domain` to group related `.play` files under a business domain. Its qualified name identifies the domain a file belongs to, typically the business area containing its bounded context. The [module](slices.md#module) names that bounded context within the model.

## Syntax

```screenplay
domain <Qualified.Name>
```

## Rules

Put `domain` first in the file, before imports or any other declaration. It is optional: a file without a `domain` line belongs to no domain. Declare it at most once per file; a second `domain` line is a compile error.

## Example

```screenplay
domain Sales

import Customers.CustomerRegistered

module Invoicing
  ...
```

Here, `domain Sales` groups the file under Sales, while `module Invoicing` names its bounded context. The unquoted `import Customers.CustomerRegistered` refers to a contract from another bounded context. To bring in other `.play` files of the same application, quote a path or glob instead; [Imports](imports.md) explains the two forms.
