---
title: Screenplay processing purposes
description: Record why an application processes personal data, check purpose coverage in Screenplay, and generate an inventory for a controller to review.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Screenplay processing purposes record why an application uses personal data and let you inspect which slices are covered by those declarations. A concept's `pii` marker identifies a personal-data value wherever it appears; a purpose describes a particular use of that data.

Start by marking the relevant [concepts](concepts.md), declare the purposes, then reference them from the modules, features or slices that use the data. The controller, the organization deciding why and how the data is processed, supplies the legal basis and retention decisions. Screenplay can check coverage and generate an inventory for review; it does not make those decisions.

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

All fields are optional in source syntax. A purpose has no executable meaning: binding reports `PLAY0270` information and adds no executable-model bytes or version. These declarations do not establish lawfulness, implement consent or retention, or change Chronicle encryption and erasure. They also do not grant access to commands or queries; use [policies](policies.md) for authorization.

## Fields

Article references below are to the General Data Protection Regulation (GDPR). The `authorization` field records a declared authorization in law for criminal-offence data; it is unrelated to a policy's `authorize` gate.

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

Write repeatable `purpose <Name>` references in module, feature and slice bodies. In the example, `RecordContact` is covered by `Billing` because its enclosing `Finance` module references that purpose. A slice's coverage is the union of its own references and those of every enclosing feature and module. Folder assembly accumulates references and keeps each name once. Unlike authorization gates, these references collect declared purposes rather than conditions a caller must pass.

With the [Screenplay CLI](tool.md) installed, use `screenplay <path> --check purposes` to inspect coverage. The check warns on:

- A slice carrying `pii` concepts through command, event, read-model or query fields, including composite types, without a declared purpose in scope.
- A covered special-category concept without a purpose's Art. 9(2) condition.
- Covered criminal-offence data without a purpose's authorization.
- A purpose without a basis, or a purpose declared but never referenced.

Review each finding against the processing the application actually performs. Ordinary compilation does not run this check, and a clean result is not a legal verdict. See [Completeness checks](completeness.md) for selections, scoped results and `--warnaserror`.

MCP `declaration-details` includes direct purpose references in container summaries and purpose fields in a purpose summary. Purpose rename is not a dedicated typed refactoring; edit its declaration and references together in an abstract syntax tree (AST) proposal tied to the source revision.

## Record of processing

Once purposes are declared and referenced, generate a controller inventory with the [installed CLI](tool.md). Supply the controller's name and contact details explicitly; the report does not infer them from application names:

```bash
screenplay report processing Samples/Invoicing --format markdown --controller-name "Example controller" --controller-contact "privacy@example.test"
screenplay report processing Samples/Invoicing --format json
screenplay report processing Samples/Invoicing --format csv
```

The default is Markdown. JSON has report-level `notice`, `coverage`, controller fields and `rows`. CSV is a regular quoted table with one data row per purpose; collection-valued cells are JSON, and controller fields and the exact notice repeat in each data row. Markdown places the notice above the table. An unused purpose still has a row with empty derived categories and slice coverage; zero purposes means zero rows, not a compliance verdict.

Each row includes declared descriptions, basis/reference, legitimate interest, condition/reference, authorization, subject categories, recipients, transfers/safeguards, retention and erasure exception. Derived columns contain reachable personal-data concept names, special categories, criminal-data presence, marker-declared security mappings and qualified covered slice addresses. Imports, opaque code and runtime protection are not inferred.

Special-category or criminal data prompts you to assess whether a data protection impact assessment (DPIA) is required. Art. 35(3)(b) concerns large-scale processing; the model does not declare scale. An erasure exception combined with reachable `pii` produces a finding: Chronicle's per-subject crypto-shredding also destroys data kept for that purpose. The report does not choose a retention strategy or implement a legal hold.

MCP `processing-record` returns the same facts as revision-bound, count/byte-bounded row pages. Supply optional `controllerName` and `controllerContact`; they are not inferred from the domain or module name. Continue with `offset` and `expectedSourceRevision` from the first response, repeating controller inputs if needed. Invalid source prevents report generation; executable binding is not required. See [MCP reference](mcp/reference.md).

When updating an existing model, move purpose, basis and retention prose out of concept reasons by hand. Keep each concept's reason focused on why the value identifies a person or is an operational secret. Repairs do not guess legal content. See [Concepts](concepts.md) and [decision 0041](https://github.com/Cratis/Screenplay/blob/main/decisions/0041-personal-data-secrets-and-processing-purposes.md).
