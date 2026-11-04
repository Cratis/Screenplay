---
id: 0136
title: Collection sample values are explicit nested pack rows
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-04
decider: woksin (decision comment on #1000)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

A sample-data `row` can only carry scalar literals, and the validator rejects every collection property (`PLAY0711`). A read model such as `BasketContents` with `lines BasketLine[]` therefore has no authorable preview content, and a Scene table bound to `rows from query AllBaskets.0.lines` stays empty. The owner ruled on [Screenplay#1000](https://github.com/Cratis/Screenplay/issues/1000) that collection sample values are defined as explicit nested pack rows, and asked for the grammar and the Scene conversion contract to be recorded first.

## Decision

**Grammar.** Inside a `row`, a bare `<field>` line (an identifier with no `=`) opens a collection value. Its children are only `row` blocks. Each child holds the same `<field> = <literal>` lines, or a further bare collection field, recursively. A bare `<field>` with no children is an explicit empty list. Leaving the field out still means the value is generated. Repeating a field name reuses `DuplicateSampleDataField`; a non-`row` child reports the existing unknown-directive diagnostic; other invalid lines keep `PLAY0706`.

**Admission.** Only a property whose type is a collection (`T[]`) of a declared `type` or of a read model accepts nested rows; each child row is validated against the element type with the existing `PLAY0710` (unknown field) and `PLAY0711` (type mismatch). Scalar lists such as `String[]` and single nested objects stay out of scope, and a scalar on a collection keeps the existing `PLAY0711` message. Nested rows on anything else report `PLAY0711`. No new diagnostic code is added.

**Syntax shape.** `SampleDataValueKind.Rows = 4` is added. `SampleDataFieldSyntax` gains an init-only `Rows` (`IEnumerable<SampleDataRowSyntax>`, default empty), with `Value` null for a Rows field. Syntax JSON omits `rows` when empty, so legacy bytes are unchanged. The positional constructor and `Deconstruct` stay as they are. The printer writes the bare field followed by indented `row` blocks, and prints source back unchanged.

**Scene conversion contract.** A consumer converts a Rows field to a JSON array of row objects, using the same rules for each child recursively. The pack id, shape and resolution order are unchanged. A consumer must fail on a value kind it does not know instead of emitting an empty string. Cross-repo reference, not changed here: Stage `Source/Contracts/Scene/SampleDataConverter.cs`.

**ESM and TypeScript.** No ESM change: sample data is design time and deferred (`PLAY0269`), outside the executable model. The TypeScript compiler keeps skipping `sample` as an opaque block and gains no pack parser.

**Versioning.** Screenplay minor: the change is additive (an enum member and an init property).

## Alternatives and consequences

Parent-linked child shapes were rejected because they need key derivation rules and a join contract. Deriving child rows only in Scene was rejected because the preview data would be non-deterministic and not authored. An inline `= [` list or a `rows` keyword was rejected because it adds tokens for no benefit; the bare-field header follows the existing `shape`/`row` block style. A separate field node type was rejected because it would change `SampleDataRowSyntax.Fields` and break constructors. New diagnostic codes were rejected to avoid golden contract churn.

Previously a bare identifier line reported `PLAY0706`; only single-identifier lines change meaning, and specs must pin this. Until a consumer such as Stage converts `Rows`, a pinned older consumer may preview an empty value; consumers should pin the new Screenplay version and land the conversion promptly. Recursion is unbounded but authored; a depth cap can be added later as a diagnostic.

## Downgrade path

Older tools reject syntax JSON containing `rows` because their reader refuses unknown members, and they report the bare field line as `PLAY0706`. Before downgrading, remove nested rows from sample packs. Legacy transport bytes for packs without nested rows are unchanged.
