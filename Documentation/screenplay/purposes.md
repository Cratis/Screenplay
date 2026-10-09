---
title: Processing purposes
description: Declare processing purposes and inspect personal-data coverage without claiming legal compliance.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Processing purposes

Declare why you process personal data once, then reference that purpose on the modules, features and slices that process it. A concept's `pii` marker describes the value wherever it appears; a purpose records the controller's declarations about a particular use.

```screenplay
concept ContactName : String pii
  pii reason "Identifies the customer's contact"

purpose Billing
  description "Issue invoices and collect payment"
  basis contract
  subjects customer, customerContact
  retention "Until the customer relationship ends"
  recipient "Payment provider"

module Finance
  purpose Billing
  feature Invoices
    slice StateChange RecordContact
      command RecordContact
        name ContactName
```

All fields are optional in source syntax. A purpose has no executable meaning: binding reports `PLAY0270` information and adds no executable-model bytes or version. These declarations do not establish lawfulness, implement consent or retention, or change Chronicle encryption and erasure.

## Fields

| Body line | Value | Cardinality |
|---|---|---|
| `description` | Quoted text or an indented `text` fence | At most one |
| `basis` | Art. 6(1) basis, optionally followed by a quoted legal reference | At most one |
| `interest` | Quoted legitimate-interest statement | At most one |
| `condition` | Art. 9(2) condition, optionally followed by a quoted reference | At most one |
| `authorization` | Quoted authorization in law for Art. 10 criminal-offence data | At most one |
| `subjects` | Comma-separated open identifiers for categories of data subjects | At most one line |
| `retention` | Quoted period or criteria; not enforced | At most one |
| `recipient` | Quoted recipient category | Repeatable |
| `transfer` | Quoted destination followed by `safeguard` and quoted safeguard | Repeatable |
| `erasure exception` | Art. 17(3) exception | At most one |

The closed vocabularies are case-sensitive:

- **Basis:** `consent`, `contract`, `legalObligation`, `vitalInterests`, `publicTask`, `legitimateInterests`.
- **Condition:** `explicitConsent`, `employmentLaw`, `vitalInterests`, `notForProfit`, `madePublic`, `legalClaims`, `substantialPublicInterest`, `healthCare`, `publicHealth`, `research`.
- **Erasure exception:** `expression`, `legalObligation`, `publicTask`, `publicHealth`, `archiving`, `legalClaims`.

```screenplay
purpose Bookkeeping
  basis legalObligation "Bokføringsloven § 13"
  retention "Five years after the financial year ends"
  erasure exception legalObligation
  recipient "Tax authority"
  transfer "United States" safeguard "EU Standard Contractual Clauses"
```

A second singleton field or an unknown vocabulary value is an error. An unresolved reference warns. `interest` without `basis legitimateInterests`, or that basis without a nonblank interest statement, warns during ordinary compilation.

## Coverage and completeness

Write repeatable `purpose <Name>` references in module, feature and slice bodies. A slice is covered by the union of its references and those of every enclosing feature and module. This is not authorization's AND rule. Folder assembly accumulates references and keeps each name once.

Use `screenplay <path> --check purposes` to inspect coverage. The check warns on:

- A slice carrying `pii` concepts through command, event, read-model or query fields, including composite types, without a declared purpose in scope.
- A covered special-category concept without a purpose's Art. 9(2) condition.
- Covered criminal-offence data without a purpose's authorization.
- A purpose without a basis, or a purpose declared but never referenced.

These are prompts to investigate, not a legal verdict. Ordinary compilation does not run this check. See [Completeness checks](completeness.md) for selections, scoped results and `--warnaserror`.

MCP `declaration-details` includes direct purpose references in container summaries and purpose fields in a purpose summary. Purpose rename is not a dedicated typed refactoring; edit its declaration and references together in a revision-bound AST proposal.

Move purpose, basis and retention prose out of concept reasons by hand. Repairs never guess legal content. Concept reasons keep only why a value identifies a person or is an operational secret. See [Concepts](concepts.md) and [decision 0041](https://github.com/Cratis/Screenplay/blob/main/decisions/0041-personal-data-secrets-and-processing-purposes.md).
