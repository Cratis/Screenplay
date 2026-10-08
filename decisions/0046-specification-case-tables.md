---
id: 0046
title: Run one specification over named cases, expanded in the front end
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/SpecificationParser*.cs
  - Source/DotNET/Screenplay/Parsing/ExpressionParser.cs
  - Source/DotNET/Screenplay/Syntax/Specifications/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/SemanticCompilation*.cs
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticSpecificationRunner*.cs
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs
  - Source/DotNET/Screenplay.Mcp/McpFixture*.cs
  - Source/Screenplay/Compiler/Parsing/Specification*.ts
  - Source/Screenplay/Compiler/Parsing/ExpressionParser.ts
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/syntaxes/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/diagnostics.md
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0032](0032-expand-typed-specification-examples-in-the-front-end.md) removed repeated instances, but scenarios that differ only in one or two input values still need one specification each, restating every step. [#449](https://github.com/Cratis/Screenplay/issues/449) asks for one specification run over rows, each with a stable name and identity in MCP and runner reports. 0032 excluded scenario outlines, and `specifications.md` says they are unsupported.

**Vocabulary.** In 0032 and [0037](0037-routes-in-redelivery-locators-and-specification-examples.md) an *example* is a named typed fixture used in a type slot. This record never calls a row an example; rows are **cases**, and a specification with them is a **table**.

**Groundwork on main.** `SpecificationExamples.Expand` with `Authored`/`Example`/`Override` provenance, the compilation-bound runner overload `Run(compilation, id)`, MCP `find-fixtures`, and the runner's `EnrichFailures` (#554). A specification's identity is `SemanticAddress.ForSpecification(slice, name)` and its name is an identifier. Executable values must be concrete. [0004](0004-admission-and-governance-of-portable-executable-semantics.md) and [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) govern any ESM change; this record needs none.

**Branch order.** The #490/#491 work (route lines in examples, [0037](0037-routes-in-redelivery-locators-and-specification-examples.md)) changes the same parser, effective-syntax and MCP files in another session and lands first. This record builds on it, and diagnostic codes are allocated one at a time across in-flight branches.

## Decision

A specification that declares parameters and cases is a **table**. It expands in the front end into one ordinary specification per case and never reaches the ESM as a table.

```screenplay
example AcmeInvoice : RegisterInvoice
  customer = "Acme"
  currency = "EUR"

specification RegisteringTotals
  description "Totals keep their currency"
  parameter total Decimal
  parameter currency Currency
  case Small total = 100
    currency = "EUR"
  case Large
    total = 1000000
    currency = "NOK"
  when AcmeInvoice
    total = case.total
    currency = case.currency
  then InvoiceRegistered
    total = case.total
```

This binds exactly as two hand-written specifications, `RegisteringTotals_Small` and `RegisteringTotals_Large`, would. No pipe tables: they need a column-aligning printer and give rows no names.

### Declarations

- `parameter <name> <Type>` declares one typed value. Unlike a behavior parameter the type is required, and it uses the property-line type grammar, collections included. Names are unique within the specification.
- **`optional` accepts only a literal `null`.** An `optional` parameter says a case may state `null`; it supplies no default, and every case still assigns every parameter.
- `case <Name>` declares one row. The name is an identifier, unique within the specification. The body assigns every declared parameter exactly once; one assignment may follow the name inline and the rest are indented.
- **Values are concrete:** literals, `null` (only for `optional`), or single-line structured objects and lists. Mapping expressions, `$` values, example names and `case.` references are refused. There are no defaults, as in 0032.
- **At least one case is required.** Parameters without cases are an error, and so is a case without a parameter.
- Parameters and cases may appear anywhere in the body. The printer writes parameters, then cases, then steps ([0013](0013-equivalence-for-screenplay-code-round-trips.md)).

### References

`case.<name>` is its own expression node, `CaseValueExpressionSyntax(Parameter)`, and fills a whole value position. It is **reserved only in specification value positions**. There is no `@case.` escape: the `@` is kept by the mapping-source parser, so `@case.x` could never name a concept anyway.

- **Allowed:** the right-hand side of any step assignment (`given`, `given readmodel`, `given capture`, `when`, `when append`, `when trigger`, `when capture`, `when query` arguments, `then`, `then readmodel`, `then query` arguments and results); `for` values, including `then no readmodel <ReadModel> for`; generated fixtures; a nested `streamId =`; and the message of **`then error`**.
- **`then error case.<name>`** takes a `String` parameter. It expands to the same quoted message, so a case value of `"$strings.key"` keeps its symbolic-key meaning. Validation-message tables are the classic use, and the change is one extra operand beside the quoted form in both compilers.
- **Excluded in v1:** `given caller` (a caller is an authentication, role and repeated-claim block, not one value, so role matrices are written as separate specifications; a `String` parameter for a `role`/`claim` value can be added later without changing anything here), `when redelivered` locators, clock instants, type, example and step-kind slots, keywords, and members inside a structured literal (pass the whole structured value as a parameter).
- Case values apply after example resolution and step overrides. A `case.` value on a step that names an example overrides that example's property like any other step value. A case cannot name an example in v1.

**Typing.** The parameter's type must be the target's declared type, or a concept and its underlying primitive in either direction. An `optional` parameter may feed only an optional target; checked once at the reference. Every case value is checked against its parameter by the ordinary fixture normalizer, including values of unused parameters (0032). The substituted value is then checked against its target exactly as a hand-written value, and the diagnostic names the case and the parameter.

### Expansion and identity

- Each case expands into an ordinary specification named `<Specification>_<Case>`, at the table's position, in case order. Samples have no `_` in specification names, so the separator is free. The names are identifiers, as generators need.
- A derived name that collides with another specification in the slice or specification-only document, another table's derived names included, is an error at the case.
- **Derived specifications inherit the table's `description`.** (Specifications take no `documentation`, per 0035.)
- Binding produces **ESM bytes and a revision identical** to hand-written specifications with those names. Models without tables are unchanged. Each derived specification has its own semantic address and catalog entry; the authored table is one syntax declaration.
- Derived names become catalog addresses and test names, so the separator and derivation are as costly to change as the keyword.

### Interplay with routes (0037)

A step's route (`stream …`, `no stream`) replaces the example's route as a whole, and `streamId = case.m` therefore means restating `stream Source.Stream` in the step. A route written in a case value position is checked per expansion, and diagnostics are **reported once per case value location**, naming the case, not once per derived step that reuses it. Contextual checks (PLAY0547, PLAY0550, PLAY0551) run on each effective step, as 0037 decides, and report at the authored step.

### Effective syntax, MCP and runner

**Syntax.** `SpecificationSyntax` gains `Parameters` (`SpecificationParameterSyntax(Name, Type)`) and `Cases` (`SpecificationCaseSyntax(Name, Values)`), both omitted by the writer when empty so untabled models keep their bytes and older strict readers reject only tables.

**C# effective API.**
- `EffectiveSpecification` gains `Case` and `SpecificationValueOrigin` gains `Case`, with `EffectiveSpecificationValue.CaseParameter`. The enum gains a member, so exhaustive consumers (Stage's converter) must handle it.
- **The singular `SpecificationExamples.Expand(SpecificationSyntax, …)` returns one effective specification and cannot express N cases. It refuses a table with a diagnostic.** A plural overload returns every effective specification of a declaration, and `EffectiveSpecificationApplication.Specifications` uses it. Stage's `ApplicationSet.ExpandSpecification` and `CratisRenderer` move to the plural overload; Stage gets an issue.

**TypeScript.** `expandSpecificationExamples` expands cases with identical derived names and returns the table and case name for each derived specification, as minimal provenance (it returned none before).

**MCP.** All additions change the golden contract of [0039](0039-publish-a-machine-readable-screenplay-contract.md), which the implementing PR regenerates.
- `declaration-details` on a specification: `summary` adds `parameters` and `caseCount`; a new `cases` view pages `{ name, effectiveName, effectiveAddress, location, values }`.
- `find-fixtures`: an optional `case` argument; `specification` accepts the table or an effective address; results add `table` and `case`; `origin` adds `"case"` with `caseParameter`.
- `find-references` links `case.` values to their parameter. `search-declarations` returns a table once, and a derived name resolves to it with `case` set. `find-assertion-gaps` treats a table as one specification. `propose-rename` of a table, case or parameter updates references and moves each derived identity.
- **Scope selection.** Scope matching is `StartsWith(scope + ".")`, so a table address would never match its `…Spec_Case` addresses. **A table address selects all its cases** in `run-specifications` scope and in `screenplay test --filter`; a derived address selects one.

**Runner.**
- `SemanticSpecificationRun` is unchanged. **Failures are reported with the case name**: `Case '<Case>' of '<Specification>':`, with the derived address as the MCP key. `Run(plan, id)` sees only the derived name and invents nothing.
- `EnrichFailures` adds provenance when any step has an example **or the origin is `Case`**; without that condition a table with no examples would lose its case provenance.

**Board.** `expandSpecificationExamples` feeds the board, and the board titles cards from that provenance; Monaco hover uses a `case` origin. A table shows as N derived specification cards, each titled `<Specification> — <Case>`, in case order, with no grouping container in v1.

### Diagnostics (all new codes)

**Errors:** a malformed `parameter` or `case` line; a duplicate parameter or case; a table without cases or cases without parameters; a case that omits, repeats or invents a parameter; a non-concrete or ill-typed case value; `case.<name>` naming an undeclared parameter, used outside a table, inside an example body or in an excluded position; an `optional` parameter into a required target; a derived-name collision; an incompatible parameter type.

**Warning:** a declared parameter that is never referenced. Samples keep the zero-warning gate, so they use every parameter.

### Grammar collisions

- `case.<name>` already parses as `PathExpressionSyntax`. In a specification value a path binds only as an enum member, bare or qualified by its concept, so the only break is an enum concept literally named `case`, whose qualified form is no longer writable in specification values; the bare member form still works. Outside a table the reference is now an error at parse time, not a late binding failure.
- `parameter` already exists in named behavior bodies with an optional type; the contexts never nest, and Monaco completion and hover key on the enclosing block.
- `case` and `parameter` as the first word of a specification body are unknown directives today, so no source breaks. A step line `case = 5` is still an assignment to a property named `case`.

## Options considered

- **Named case blocks, typed parameters, front-end expansion (chosen).** It reuses the assignment grammar, typed checks and 0032's expansion, and names every row.
- **`$case.<name>`.** The close runner-up: nothing collides, and VS Code needs one more word. Not taken because `case.` reads like its declaration keyword. Switching later would cost every sample and corpus use.
- **Gherkin pipe tables, `<param>` placeholders, bare parameter names, `row.`, `$example.`.** Rejected: untyped or colliding, or they reuse the word "example".
- **Keeping the table in the ESM.** Rejected: it needs a 0004 admission and a 0025 version for no execution gain.
- **Index-derived or bracketed names.** Rejected: unstable under reordering, or not identifiers.
- **Excluding `then error`.** Rejected: it is the most common use and cheap to allow.
- **Allowing `given caller`.** Deferred: it is a block, not a value; cheap to relax later.

## Default if unanswered

Specifications keep restating every scenario, and `case.x` stays a path that fails late at binding.

## Timeline and scope

From acceptance until superseded, after the #490/#491 work lands. It lifts 0032's scenario-outline exclusion for this form only.

**Out of scope:** per-case steps or outcomes, case descriptions, tags and skipping; cases naming examples; references inside structured literals; clock substitution; Cartesian products and cases loaded from files; any ESM change.

| # | Size | Lands on |
| --- | --- | --- |
| 1 | M | C# syntax, parser, printer, walker, syntax JSON, schema and golden, parse diagnostics |
| 2 | M | TypeScript parity and conformance vectors (`specification-tables.play`, `diagnostics.json`) |
| 3 | M | Expansion, plural overload, binding diagnostics, derived addresses, equivalence specs |
| 4 | M | MCP, runner, scope selection, `screenplay test --filter`, workspace rename |
| 5 | S | Monaco, VS Code grammar, board, `specifications.md`, `diagnostics.md`, one sample |

Every surface lands in **one pull request to main**, as `.cratis/ai/rules/project/language-change-surface.md` requires: both compilers, expansion, runner and MCP selection of table addresses, rename, editors, docs and the Invoicing sample. The slices above are internal build steps; they may be built on sub-branches merged into an integration branch first, but nothing merges to main before all of them are done. Downstream issues: Cratis/AI (`cratis-screenplay-specifications`, `cratis-screenplay-scenario-coverage`), Stage (plural overload, `Case` origin), and Studio's importer.

## Verification

**Done when:**
- Tables parse, print and round-trip identically on both compilers.
- Expansion yields derived specifications with case provenance, and every diagnostic fires precisely.
- Tabled and hand-written models have identical ESM bytes and revisions; existing goldens and corpus outcomes are unchanged.
- The singular `Expand` refuses a table; the plural overload expands it.
- A failing case reports its name; a table address selects every case in `run-specifications` and `--filter`.
- `then error case.<name>` expands and compares like the quoted form.
- A route in a case value reports once per case value location.
- MCP, rename, editors and the board expose cases.

**Verify by:** parser, printer, syntax JSON and conformance vectors; expansion and provenance specs; binder equivalence specs against hand-expanded documents; MCP and runner specs; the repository's .NET and TypeScript gates.

## Consequences

One scenario shape covers many inputs while every derived fact stays visible. Editing a step changes every case, which the `cases` view and hover make visible. Consumers with exhaustive `SpecificationValueOrigin` switches, and Stage's singular expansion, must change. An enum concept named `case` loses its qualified form in specification values.

## Related

Screenplay [#449](https://github.com/Cratis/Screenplay/issues/449), [#490](https://github.com/Cratis/Screenplay/issues/490), [#491](https://github.com/Cratis/Screenplay/issues/491). Decisions 0004, 0013, 0016, 0025, 0031, 0032, 0035, 0037, 0039.
