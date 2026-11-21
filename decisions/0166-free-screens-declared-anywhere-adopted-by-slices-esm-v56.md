---
id: 0166
title: Screens are declared anywhere and adopted by slices, which provide the data they need (ESM v56)
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-15
decider: Direct request for the Scene Design epic (Cratis/Scene#323), issue Cratis/Screenplay#1319
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A screen has been a member of exactly one slice: it is declared in the slice's body, reads the slice's read models and is
rendered for that slice. A screen a designer draws first - an invoice list, a dashboard, a details page - cannot exist before
the slice that serves it, cannot be shared by two slices, and cannot be navigated to from a storyline until a slice carries it.

Scene Design starts from the screen. It needs a screen that stands on its own, states the data it needs without naming where
the data comes from, and is later adopted by the slice that provides that data. [0149](0149-screens-in-the-executable-model.md)
admitted slice screens to the executable model as ESM v42; nothing in it forbids a screen that no slice declares.

## Decision

### S1. A screen is declared anywhere

`screen` is one construct that may be declared at the top level of a document, in a module, in a feature or in a slice. A
screen a slice declares in its own body is that screen declared in the slice scope and adopted by that slice, so existing
models, their printing and their ESM v42 identities are unchanged. A free screen is printed where it was authored; a screen
in a folder-composed model is read from whichever file declares it.

A screen is declared once in a scope (`PLAY1152`). Two scopes may declare screens of the same name; a reference resolves from
where it is written, then by unique match (S3).

### S2. A slice adopts a screen with `uses screen`

```screenplay
slice StateView ListInvoices
  uses screen InvoiceList
    data Invoice via query ListInvoices
      number from invoiceNumber
```

The adoption is many to many: a slice adopts several screens, a screen is adopted by several slices. Hooks (`button ... slice X`)
and "the slice's screen" for a State Change input work on an adopted screen exactly as on one declared inline. The adopting slice
is the slice the UI contract names for the screen. A slice adopts a screen once (`PLAY1154`); every provision goes under that line.

### S3. A screen resolves its names from where it is declared

A name in a screen - a query, a command, another screen, a read model - resolves inside-out from the screen's **declaration
site**, then by unique match anywhere in the model. An ambiguous or unresolved name stays a warning, as it is for a slice
screen. An adopter never changes what a name in the screen means.

### S4. `data <Shape>` without `via query` is a need

```screenplay
screen InvoiceOverview
  data Invoice[] as overdue
  data Invoice[] as open
```

`data <Shape>[]` or `data <Shape>` with no `via query` declares a **need**: data the screen reads and an adopting slice
provides. The shape is a contract with two sources.

- **Inferred.** The fields the screen reads of it - table columns, summary fields, the `by` key of a row click, bound
  `$data` paths - are the contract the compiler derives.
- **Declared.** A top-level `shape` declaration tightens the inference with typed fields:

```screenplay
shape Invoice
  description "What the invoice list reads"
  number InvoiceNumber
  amount Decimal
  dueDate Date optional
```

A shape's fields are written like a read model's properties, but a shape has no key, identifier, subject or generated modifier:
it is a contract on data, not a stored model (`PLAY1149`). A shape is declared once (`PLAY1151`) and only at the top level of a
document.

**The alias.** A screen that needs the same shape twice tells the needs apart with `as <alias>`; the alias is the need's name
in the screen's own directives (`table overdue`) and in the provision (`data overdue via query ...`). A need without an alias
is named by its shape. Two needs with one name are refused (`PLAY1153`).

### S5. The adopting slice provides each need

`data <Need> via query <Query> [by <param>]` under `uses screen` provides one need. A nested `<field> from <path>` maps a
shape field to a read model path where the names differ; a field whose name matches is mapped implicitly. The compiler checks
that:

- the query exists (`PLAY0196`) and is one query of the slice or a unique match;
- a list need is provided by a list query and a single need by a single-value query (`PLAY1159`);
- every field the screen reads, and every field its `shape` declares, is a property of the query's read model, by name or
  by a `from` mapping (`PLAY1160`), and has a compatible type when the shape declares one (`PLAY1161`; a concept stands for
  its underlying primitive);
- `by` names a parameter the query declares (`PLAY1162`);
- a mapping names a field the screen reads or the shape declares (`PLAY1163`);
- the provision targets a need the screen has (`PLAY1156`), never one the screen already sources with its own
  `data <ReadModel> via query` (`PLAY1157`), and provides it once (`PLAY1158`).

### S6. Severities

An adopted screen with an unprovided need is a **warning** (`PLAY1155`). It fails wherever warnings are errors - the check
command's `--warnaserror` release gate and a gate file's `warningsAsErrors` - and executable screen admission refuses it with
a reason that names the screen and the shape. A screen no slice adopts is **design-only** (`PLAY1164`, information): it never
blocks the rest of the model and never reaches the executable model or the UI contract.

### S7. Actions and navigation have no contract

A command or a screen a free screen names resolves by unique match when the model arrives (S3); until then it is a warning.
Nothing in the screen states what the adopter must provide for an action.

### S8. Sample data may name a shape

`sample data ... shape <Name>` may name a declared `shape` as well as a read model. Its rows are checked against the shape's
typed fields, as they are against a read model's properties.

### S9. Storylines, navigation and `show` reach any screen

A storyline `screen` step, a `navigate to` and a `show` resolve to any screen, free or declared in a slice.

## ESM v56

Free screens are admitted into the executable model as ESM v56, numbered at admission under
[0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md). Admission stays opt-in, as in
[0149](0149-screens-in-the-executable-model.md).

- The application carries `screens`, the free screens that at least one slice adopts. A screen's identity is the application
  plus a declaration key qualified by the module and feature it is declared in, so two same-named free screens keep distinct
  identities. A free screen not declared at the top level carries its dotted `scope`.
- A free screen carries `needs`: the name, the shape, whether it is a list and the fields the screen reads.
- A slice carries `adoptions`: the identity of the adopted screen and a `provisions` array holding, per need, the providing
  query, its optional `by` and the `fields` whose read model path differs from the field name.
- A slice's own `screens` are unchanged and never carry needs: a need nothing can provide is an error in the model.
- Admission refuses a model with an unprovided need, a provision of an unknown need, a duplicate adoption and a provision
  whose query does not return what the need asks for, each with a reason naming the screen and the shape.
- A model without a free screen is written and read exactly as before: ESM v42 and every later version that predates this
  one keep their bytes, and the binder selects v56 only when a free screen is admitted.

## UI contract

`screenplay-ui/1` is unchanged in shape. A free screen reaches it once per adopting slice: the contract screen names the
adopting slice (`slice`, `sliceId`) and carries the data the slice's provisions resolve, with the read model paths mapped back
to the screen's fields. A screen no slice adopts is not part of the contract.

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY1149` | Error | A `shape` declaration is malformed or misplaced. |
| `PLAY1150` | Error | A `uses screen`, a provision or a mapping is malformed. |
| `PLAY1151` | Error | A shape is declared more than once. |
| `PLAY1152` | Error | A screen is declared more than once in a scope. |
| `PLAY1153` | Error | Two needs of a screen share a name. |
| `PLAY1154` | Error | A slice adopts the same screen more than once. |
| `PLAY1155` | Warning | An adopted screen has a need the slice does not provide. |
| `PLAY1156` | Error | A provision names a need the screen does not have. |
| `PLAY1157` | Error | A provision overrides a source the screen already resolves. |
| `PLAY1158` | Error | A need is provided more than once. |
| `PLAY1159` | Error | A provision's query returns a list where a single value is needed, or the reverse. |
| `PLAY1160` | Error | A field the screen reads is not a property of the providing read model. |
| `PLAY1161` | Error | A read model property has another type than the shape declares. |
| `PLAY1162` | Error | A provision's `by` is not a parameter of the query. |
| `PLAY1163` | Error | A mapping names a field the screen does not read and the shape does not declare. |
| `PLAY1164` | Information | A free screen no slice adopts is design-only. |

## Compatibility

- A screen declared in a slice parses, prints, binds and serializes exactly as before. `Source/Screenplay/Compiler/Conformance`
  keeps the Library and Invoicing golden syntax for every construct that existed; the added members are written only when present.
- The syntax tree adds `ShapeSyntax`, `UsesScreenSyntax`, `ScreenProvisionSyntax`, `ScreenProvisionMappingSyntax` and
  `ScreenNeedSyntax`, and the `Shapes`, `Screens` and `UsedScreens` members, as `init` properties with empty defaults, so no
  positional record of the public tree changes.
- `data <Name>` alone was a malformed data directive (`PLAY0104`); it is now a need. `data <Name> via query` with no query is
  still `PLAY0104`.
- ESM v42 through v54 identities, bytes and golden vectors are unchanged.

## Consequences

Scene Design can start from a screen, show it with sample data against a shape, and hand it to the slice that serves it. The
cost is a second way to reach a screen's data - a provision - which the compiler validates in full, so an adopter cannot
silently feed a screen data of another shape.
