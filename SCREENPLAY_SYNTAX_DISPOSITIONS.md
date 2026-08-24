<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay Program v1 syntax dispositions

This matrix prevents `ApplicationSyntax → ESM` binding from silently dropping, strengthening, or reinterpreting source syntax. It describes the Program v1 backend semantic profile, not removal from the language. Source parsing, printing, and compatibility remain unchanged.

| Disposition | Meaning |
| --- | --- |
| `bind` | Resolve to equivalent portable ESM semantics. |
| `preserve_legacy` | Keep the existing source meaning; do not strengthen it into new semantics. ESM execution remains blocked until an explicit migration exists. |
| `report_only_realization` | Preserve as source/realization/operational evidence outside portable semantic behavior. |
| `block` | Emit a typed binding error because omission or approximation would change behavior. |
| `migrate` | Require an explicit reviewed transform and semantic-version decision. |
| `explicit_defer` | Preserve source compatibility but exclude the construct from the current backend profile with a visible disposition. |

## Application and structure

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| `ApplicationSyntax` | `bind` | Application name is supplied by the binding request; source files do not invent it. |
| `DomainSyntax` | `report_only_realization` | Authoring/grouping metadata; not runtime behavior. |
| `ImportSyntax` | `block` | Cross-document declaration binding is not admitted in the first vertical. |
| `ConceptSyntax` primitive/enum shape | `bind` | Bind primitive representation and declared enum values. |
| concept `@pii` / `@sensitive` and reasons | `block` | Portable data-subject relationships and capabilities belong to #141. |
| concept/type/slice/event/read-model/projection/specification/trigger `File` | `report_only_realization` | Placement/provenance; path is never semantic identity. Implementation-bearing file references follow #139. |
| concept validation | `bind` for declarative `not empty`; otherwise `block` | Named/code/regex and richer validation wait for admitted portable contracts. |
| `TypeSyntax` and `PropertySyntax` | `bind` | Bind primitive, concept, composite, optional, and collection references. |
| descriptions | `report_only_realization` | Preserved authoring narrative outside ESM v1 revision. |
| `ModuleSyntax` / nested `FeatureSyntax` | `bind` | Preserve hierarchy and stable semantic identity. |
| layouts, screen/dialog templates, forms, contributions | `explicit_defer` | Frontend profile remains excluded. |
| themes and UI profiles | `explicit_defer` | Renderer/profile metadata, not backend semantics. |
| `PersonaSyntax` | `block` | Requires portable caller/policy semantics. |
| `AuthenticationSyntax` / providers | `report_only_realization` | Runtime integration metadata. |
| `SeedSyntax` | `report_only_realization` | Operational/bootstrap metadata. |

## Slice families

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| `StateChange` slice | `bind` | First command/event vertical. |
| `StateView` slice | `bind` | First read-model/projection/query vertical. |
| `Automation` slice | `block` | Portable occurrence/effect semantics remain #69/#73. |
| `Translate` slice | `block` | Requires a portable compiled CDL plan. |
| `ScreenSyntax` and directives | `explicit_defer` | Frontend generation/runtime excluded. |
| `ConstraintSyntax` | `block` | Requires portable constraint semantics and append specifications. |
| `ReactionSyntax` / `TriggerSyntax` | `block` | Requires admitted occurrence, time, and effect contracts. |
| `CaptureSyntax` | `block` | Requires versioned standalone CDL plan. |
| `ReducerSyntax` / reducer rules | `block` | Requires portable reducer transition and implementation contracts. |

## Commands, events, and validation

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| command properties and one `identifier` | `bind` | Required scalar identifier supplies modeled event-source destination. |
| unconditional `ProducesSyntax` | `bind` | Bind event contract, destination, and mappings. |
| conditional `produces when` | `block` | Condition semantics are outside the first vertical. |
| `ProducesSyntax.Tags` / event tags | `block` | Cross-cutting event information must not be silently lost. |
| command `AuthorizeSyntax` | `block` | Portable policies belong to #142. |
| declarative command `not empty` | `bind` | First rejection behavior. |
| command requirements and other validation kinds | `block` | Admit capability by capability with evaluator/renderer vectors. |
| `CodeValidateSyntax`, named rule body, `HandlerSyntax` | `block` | Constrained implementation requirements/attachments belong to #139. |
| `ReadsSyntax` | `preserve_legacy` | Never reinterpret as decision consistency; migration belongs to #129. |
| `ConcurrencySyntax` | `preserve_legacy` | Framework-shaped metadata; never imply portable decision consistency. |
| `EventSyntax` properties | `bind` | Event-source identity remains contextual and is not added to payload. |
| event-contract identity/revision | `bind` | Resolve through the identity catalog; initial revision only. |

## Read side

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| `ReadModelSyntax` shape | `bind` | Identifier is resolved from the admitted keyed query/projection contract. |
| simple `ProjectionSyntax` `from` transition | `bind` | One event, deterministic affected key, automap plus explicit set mappings. |
| projection `sequence` | `block` | Runtime sequence responsibility is not portable ESM v1 behavior. |
| joins, children, nested, every/all, arithmetic, clear/remove | `block` | Bind only when ESM/evaluator/renderer share equivalent semantics. |
| projection raw/unsupported expressions | `block` | Never emit target code from an unresolved expression. |
| one snapshot `QuerySyntax` with `by` | `bind` | Program v1 optional deterministic lookup. |
| observable delivery | `block` | Live delivery conformance belongs to #140. |
| filters, scope, authorization, performer | `block` | Complete query/policy/implementation semantics belong to #140/#142/#139. |

## Specifications

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| given/then events | `bind` | Exact property values in authored order. |
| given/then keyed read-model state | `bind` | Key is the declared identifier property value; never guessed elsewhere. |
| `when` command | `bind` | One command with exact values. |
| `then query`, arguments, ordered results | `bind` | Exact Program v1 query assertion from #150. |
| bare/message rejection | `bind` | Typed rejected outcome; message remains optional. |
| specification `File` | `report_only_realization` | Source realization/provenance only. |
| context/environment/raw expressions in values | `block` | Program v1 vectors require concrete portable values. |
| future query/event/time/external actions and richer comparisons | `explicit_defer` | Owned by #87; must be added through init-only compatible syntax. |

## Expressions and code

| Source syntax | Disposition | Program v1 behavior / owner |
| --- | --- | --- |
| literal string/number/bool/null | `bind` | Bind to canonical semantic values after target type validation. |
| property path resolved in command/event scope | `bind` | Bind to stable property identity. |
| `$context`, `$env`, `$strings`, source-item, raw expression | `block` | No omission, target-language evaluation, or environment guessing. |
| `CodeBlockSyntax` / implementation `FileReferenceSyntax` | `block` | Becomes a role-specific requirement/attachment only through #139. |

## Binding rule

A `report_only_realization` or `explicit_defer` entry may produce an informational diagnostic while the backend semantic compilation succeeds. A `block`, unresolved `preserve_legacy`, or required `migrate` entry prevents creation of a semantic compilation. Every newly added syntax type must update this matrix and its binder coverage before the language-changing PR can close.
