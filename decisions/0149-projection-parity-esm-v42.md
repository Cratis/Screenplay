---
id: 0149
title: Write once, key a dictionary by the event or its cause, remove a key and order children in projections (ESM v42)
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-09
decider: Einar Ingebrigtsen (cross-repository contract agreed on Cratis/Chronicle#5755 for Cratis/Screenplay#1220)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Chronicle's projection engine is gaining what a reducer is used for today ([Chronicle#5755](https://github.com/Cratis/Chronicle/issues/5755)): a value written only once, a dictionary keyed by a value of the event or of who caused it, removing such a key, and a children collection with a stored order. Screenplay owns the projection grammar, so the constructs land here first, with the forms agreed for Chronicle's definition language.

## Decision

```screenplay
projection Models => ModelReadModel
  from SlotAssigned
    slots.$eventContent.slot = series
    count assignments.$causedBy.userName
    set once createdBy = $causedBy.subject
  from SlotDropped
    remove slots.$eventContent.slot
  children entries identified by entryId
    sort by addedAt descending
```

- **Dynamic keys.** A mapping target can end in `.$eventContext.<path>` (as before), `.$eventContent.<path>` (a property of the event) or `.$causedBy.subject|name|userName`, on every mapping line except `clear`: clearing an entry would keep its key with a null value, so `clear` with a dynamic key is an error (`PLAY0963`) that points at `remove`. A malformed key is an error (`PLAY0957`); an `$eventContent` path the handled event does not have is a warning (`PLAY0961`); any other `$` source stays a warning (`PLAY0299`). The base property is what is checked against the read model, and the one automap treats as explicitly mapped. Chronicle needs the base to be a dictionary and Screenplay has no dictionary type, so a base the read model declares is a warning (`PLAY0962`); a dictionary type is a later language change.
- **`set once <target> (=|to) <value>`** assigns only when the target is missing or null. The first event that finds it empty writes it, so replay gives the same result.
- **`remove <target>`** removes the dictionary entry the target's dynamic key names; a target without a dynamic key is an error (`PLAY0956`). `remove with` and `remove via join` stay block directives; a property named `with` or `via` is written `remove @with...`, as `clear @with` already is.
- **Children ordering.** A `children` block takes at most one `insert first`, `insert last` or `sort by <path> [ascending|descending]` line (`PLAY0958` malformed or misplaced, `PLAY0959` more than one). The sort path is checked against the element type (`PLAY0960`). The order is stored: a child is placed when it is added and never moved by an update; sorting is stable, a missing value first ascending and last descending. `insert last` is what a block without an ordering does.
- **`$causedBy.<member>`** binds in the executable model to the same value as `$eventContext.causedBy.<member>`; Chronicle resolves it at runtime ([Chronicle#4119](https://github.com/Cratis/Chronicle/issues/4119), closed). A bare `$causedBy` is still refused as a value.
- **ESM v42.** A set-once mapping (`"operation":"setOnce"`) or a children ordering (`"ordering":{"placement":"prepend"}` or `{"placement":"sorted","sortBy":[…],"direction":…}`) selects 42.0. A model that uses neither keeps its version and bytes, `insert last` binds to no ordering. Dynamic keys and `remove` stay outside the executable model, which has no dictionary-keyed property; the binder refuses them as before.

## Consequences

- Both compilers, the printer, syntax JSON and its schema, the walkers, the binder, validator, serializer and reference evaluator change together; the Monaco language service and the VS Code grammar highlight the new words.
- The public syntax tree grows additively: `SetOnceMappingSyntax`, `RemoveKeyMappingSyntax`, `ChildrenOrderingSyntax` with `ChildrenPlacement` and `ChildrenSortDirection`, and `ChildrenSyntax.Ordering`. Chronicle's definition-language visitor reads them from `Cratis.Screenplay.Syntax.Projections`.
- Stage lowers the constructs to the Chronicle client in its own repository: `SetOnce`, the keyed `Set` and `RemoveKey`, and the children builder's `Prepend`, `OrderBy` and `OrderByDescending`.
