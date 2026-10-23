---
id: 0111
title: A reference input chooses from a declared query; defaults are declared once
status: accepted
stage: none
decided: 2026-10-23
decider: woksin
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A command input that holds the identifier of another entity (`TagContact.tagId TagId`,
`MoveOpportunityToStage.stageId StageId`; `Crm.play:139-141`, `:382-386`) needs a picker over the entities that exist.
Screenplay has no way to say which.

- A form field is `field <property> [from <source>|compose using <Callback>] [label]` (`forms.md:14-30`;
  `FormFieldSyntax`, `FormSyntax.cs:165`).
- Read models have typed keys, explicit or inferred (`readmodels.md:37-41`).
- Stage marks identifiers through `x-concept` (`stageFormSchema.ts:72`) but nothing links a concept to a read model and
  a list query (Stage #744).
- Scene's `FormField(Name, SourceProperty, ComposeUsing, Label, Placement)` has no options source
  (`FormField.cs:6-17`). Its `PropertyEditorKind` "reference" is a design-time editor
  (`PropertyEditorKind.ts:55`). A blueprint `Lookup` carries only string options
  (`blueprint.default/foundation/formModel.ts:5-30`).
- Components has `DropdownField` with `options`, `optionValue` and `optionLabel`
  (`DropdownField.tsx:17-24`) and a `MultiSelectField`.
- SoloCRM declares its ids as plain `Uuid` concepts (`Concepts.play:2-8`).

An identifier concept says what a value is. It does not say which query the caller may read, whether archived entities
may be chosen, which property to show, whether the reference is optional, or whether choices depend on another field.
`identifier` also marks an event-stream boundary, not "generate this value"
(`cratis-screenplay-command-surface/SKILL.md:118-124`). Choosing an existing entity, creating a new identity and binding
the command's own target are three different things.

This record is allowed by the record that lifts the freeze (0105 on branch `screenplay-1035-typed-capture-key`).

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **Explicit choice on a form field.**
   ```screenplay
   form MoveOpportunityForm for MoveOpportunityToStage
     field stageId choose from AllStages value stageId show name
     field organizationId choose from ActiveOrganizations value organizationId show name
     field ownerId choose none
   ```
   - `choose from <Query>` names a collection query that returns a read model whose key has the field's concept.
     Otherwise the compiler reports an error.
   - `value <key>` names the key property whose value is stored. It may be left out when the read model has one key.
   - `show <path>` is the property path shown to the person. It may be left out when a display property is declared
     (item 3).
   - `choose none` forces a plain input.
   - The query must be invocable with no arguments, or with arguments that a `with <argument> from <field>` line binds
     from other fields of the same form:
     `field stageId choose from StagesOfPipeline with pipelineId from pipelineId`. Every other argument must be optional
     or have a default. Otherwise the compiler reports an error. Until a bound field has a value the picker is disabled.
   - The stored value is always the identifier, never the shown text.
2. **Choice never replaces server checks.** The picker shows what the caller may read. Membership in the list is not
   authorization or relationship validation; the command still validates.
3. **A display property, declared once.** A read model property may be marked `display`:
   ```screenplay
   readmodel Organization
     organizationId OrganizationId key
     name           OrganizationName display
   ```
   Pickers, table cells that show a reference and breadcrumbs use it. At most one property per read model carries it.
   A read model with no `display` property, and a field with no `show`, shows the key. The compiler reports information
   naming the read model.
4. **A default choice, declared once.** A model may declare, for an identifier concept, the default choice query. A field
   without `choose` then uses it, for every form and for generated forms. The compiler never selects a query by
   itself: not from the concept alone, not from a `*Id` name, not from a matching key. Two defaults for the same concept
   are a compiler error. Where no default and no `choose`
   exist, the field is a plain input and the compiler reports information naming the candidate queries, with a repair
   that writes the explicit form.
5. **Collections.** An input of type `X[]` becomes a multiple choice.
6. **Scene gets the contract.** `FormField.Options` (init-only, .NET and TypeScript): the query and its argument
   bindings, the value property, the label property, multiple or single, optional, plus the search and paging
   contract, resolution of a stored value that is not on the current page, and loading, error and empty behaviour.
   Generated forms with no `form` declaration carry the same through the command schema
   (`x-reference: { query, route, value, label }`).
7. **Stage** renders the declared choice onto `DropdownField` and `MultiSelectField`. Stage #744 is re-scoped to render
   what is declared, with no inference from concepts. Stage's session identity fallback
   (`StageCommandForm.tsx:258-303`) is implementation behaviour, not part of this contract.
8. **No ESM change** for the choice itself, because form and screen syntax is authoring-only. The `display` marker sits
   on a read model, which is in the executable model. It stays authoring-only and out of the ESM bytes; whether the ESM
   writer already ignores such a member is open point 1.

## Options considered

- **Infer the picker from the identifier concept alone.** Rejected: ambiguous, can leak entities the caller may not use,
  and cannot choose the display property or archived filtering.
- **Inference plus an explicit override.** Accepted only where inference reads a declared default (item 4).
- **Explicit only, per field.** Rejected as the only form: every application of SoloCRM's size would repeat the same
  choice for each command, and authors would leave them out.
- **Put the reference on the concept (`concept OrganizationId identifies Organization`).** Rejected: it still says
  nothing about authorization, archived filtering or display.
- **A raw identifier input.** Kept as the advanced fallback (`choose none`).
- **Generate every missing identifier.** Rejected: it invents references to entities that do not exist.
- **A separate picker component.** Rejected: forms own fields.

## Consequences

- **Screenplay:** `FormFieldSyntax.Choose` and `PropertySyntax.IsDisplay` as init-only members, validation of key and
  query cardinality, the default declaration, the information diagnostic and repair, `forms.md`, `readmodels.md`.
- **Scene:** `FormField.Options` and the form runtime. **Stage:** schema synthesis and form picker, parity between the
  live Host and the rendered application. **Arc:** the picker query runs under the caller's authorization. **Chronicle,
  CLI:** none beyond bundled versions. **AI corpus:** teaches `choose from` and `display`.
- Existing forms stay valid. Suggestions may be reported; they never change existing field behaviour.
- Large lists, duplicate labels, deleted entities and cross-tenant leakage are the risks. Paging and search are the
  Scene contract; `search by` is a later spelling.

## Open points

1. Whether the ESM writer ignores an unknown read-model syntax member such as `display`. This must be checked before the
   marker is implemented; if it does not, the marker is carried outside the read-model JSON.
2. The spelling of the default-choice declaration (an application-level line naming the concept and the query).
3. Search within a choice list, and narrowing a list by anything other than arguments bound from other fields of the
   same form, are follow-ups.
4. The keywords `choose from`, `value` and `show` are open to taste; the semantics are fixed.

## Related

Builds on 0029. Stage #744; Scene `FormField`; Screenplay `forms.md` and `readmodels.md`.
