---
title: Model comparison API
description: Compare Cratis Screenplay models in C# by persisted identity or exact address to find structural changes, event contract differences and coverage gaps.
---

## Compare model structure in C#

Use `ModelComparison.Compare` from `Cratis.Screenplay.Comparison` in the
`Cratis.Screenplay` package to inspect structural changes without running MCP.
The result describes authored structure, not executable equivalence or a
specification execution verdict. To compare declared event relationships with
host-supplied registrations, appends and consumers, use the
[observation comparison API](observation-comparison.md).

This example compares two in-memory sources and finds an added event property:

```csharp
using Cratis.Screenplay.Comparison;

const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
var before = ComparedModel.FromSources("Projects",
    new Dictionary<string, string> { ["application.play"] = source });
var after = ComparedModel.FromSources("Projects",
    new Dictionary<string, string> { ["application.play"] = source + "        extra String\n" });

var difference = ModelComparison.Compare(before, after);
var addition = difference.Events.Single(change => change.Change == EventContractChangeKind.PropertyAdded);
Console.WriteLine($"{difference.Matching}: {addition.Property}, breaking={addition.ContractBreaking}");
```

The output is `Address: extra, breaking=True`. Address matching intentionally
leaves identity coverage incomplete; that does not hide the known property
change.

## Choose comparison inputs

| Factory | Input | Persisted identities |
| --- | --- | --- |
| `ComparedModel.WithIdentities(workspace)` | An immutable `ScreenplayWorkspace` with an authoritative catalog | Yes |
| `ComparedModel.WithoutIdentities(workspace)` | A workspace whose catalog must not establish continuity | No |
| `ComparedModel.FromSources(applicationName, sources)` | Portable `.play` paths mapped to complete source strings | No |
| `ComparedModel.FromWorkspaceExport(json)` | A complete canonical UTF-8 workspace export as `ReadOnlySpan<byte>` | Yes |

`Workspace` retains the source and compilation diagnostics.
`HasPersistedIdentities` records your explicit choice, not a guess based on
catalog origin. `FromSources` creates documents in memory; it does not read
paths from disk. Paths can name subfolders (`Features/Register.play`): each
document keeps its relative path and gets the same opaque key a model root
would give it. Invalid-but-editable source remains available for comparison.
Invalid document paths, workspace admission or exports raise the existing
workspace contract exceptions. Bound export input size before passing it to
`FromWorkspaceExport`; see [Workspace transport](workspace-transport.md).

## Matching modes

- **Identity:** both inputs declare persisted identities and share the same
  application identity. Renames and owner/document moves preserve semantic
  continuity. Two identity-bearing inputs from different applications throw
  `IncompatibleModelIdentities` rather than falling back.
- **Address:** every other combination, including different applications when
  either side has no persisted identities. Keys contain the normalized kind
  and all typed semantic address parts except the application. Owner kind and
  event generation remain part of the key. Display addresses remain dotted.
  Renames and owner moves appear as removal plus addition; document movement
  is not compared. `ComparedDeclaration.SemanticId` is null and `Identities`
  is empty.

When both models are executable, Address matching uses catalog addresses and
compares property-level members. If either model is not executable, both use
exact authoring declaration keys and report `DeclarationLevelOnly`.

To compare an authored model with a model generated from code, use
`WithIdentities` for the authored workspace and `WithoutIdentities` or
`FromSources` for the generated side. An automatically allocated catalog does
not make generated identities authoritative.

## Read the result

`ModelDifference` exposes `Matching`, `Complete`, `HasSemanticChange`,
`BeforeExecutable`, `AfterExecutable` and declaration group counts for each
side. `HasSemanticChange` is true when a known structural change exists, false
only when the comparison is complete with no semantic change, and null when
coverage is incomplete without a known semantic change. A document-only move
is not a semantic change.

The typed lists are:

| Property | Record | Meaning |
| --- | --- | --- |
| `Declarations` | `DeclarationChange` | Added, removed, renamed or moved declarations; moves distinguish owner from document |
| `Members` | `MemberChange` | Changed authored members or opaque content; hashes are null when a member is absent on that side |
| `Enumerations` | `EnumerationMemberChange` | Numeric identity changes within matched enum concepts |
| `Events` | `EventContractChange` | Property addition/removal/type change and generation addition/removal |
| `Specifications` | `SpecificationChange` | Added/removed specifications and effective expected-outcome or opaque changes |
| `Dependants` | `DirectDependant` | Direct indexed references from either snapshot, with role and resolution confidence |
| `Identities` | `IdentityChange` | Assigned, retired or migrated semantic/event-contract identities in Identity mode |

Each change identifies its declaration, with nullable before/after addresses.
Event property changes keep `ContractBreaking` visible even when
`GenerationCovered` is true. Event type strings are serialized syntax types,
not runtime type names. Specification example expansion is included in the
compared outcomes. Member hashes use SHA-256 of normalized authored members;
descriptions and documentation do not create semantic changes.

### Stored enum numbers

Reordering a positional enum can reinterpret stored data even when an event's property type stays the same.
`Enumerations` reports each affected baseline member. Its effective number is its explicit pin, or its zero-based
declaration position when unnumbered. `BeforeNumbering` and `AfterNumbering` distinguish `Explicit` from `Positional`,
including explicit pins equal to positions that the executable model omits.

Members match by ordinal name within a matched enum concept. Identity matching keeps a concept's catalog identity
through renames and moves; Address matching requires the exact concept address.

| `EnumerationMemberChangeKind` | Meaning |
| --- | --- |
| `NumberChanged` | A surviving name has a different number; includes positional insertion, reorder and explicit renumbering |
| `Removed` | The baseline name is gone and nobody holds its number |
| `NumberReused` | The baseline name is gone and another member holds its number, without the explicit rename evidence below |
| `Renamed` | Both enums are explicitly numbered, the baseline name is gone, and a name absent from the baseline holds its number |

`Member` is the baseline name. `AfterMember` is the surviving name or the candidate holder of the baseline number;
`AfterNumber` is that member's number. Both are null for `Removed`. Appending members, retaining explicit numbers
while reordering, or pinning positional members to their existing positions adds no enumeration records. Generic
`Members` hash changes remain visible separately.

`ContractBreaking` is true for every kind except `Renamed`, independently of storage usage. `Stored` is a separate
flag: the baseline enum is reachable from an event property, including through nested composite types. It does not
prove that events were persisted. `Renamed` is not `ContractBreaking` for the default number-based storage: a rename
keeps stored data compatible, but changes member names in generated C#/TypeScript code (a source-level API change)
and Chronicle's `x-enumNames` schema metadata. Data written with an explicit name-based converter, rather than the
default numeric converter, would also break. Direct indexed dependants identify users of the changed concept, not transitive
runtime impact.

The comparison cannot distinguish a positional rename from removal and number reuse, so it reports `NumberReused`.
An explicit removal followed by reuse under a new name is indistinguishable from an explicit rename and reports
`Renamed`; do not treat that classification as proof of compatibility. An enum concept renamed in Address mode is
removed plus added, with no member comparison. Switching between enum and non-enum is an ordinary `type` member
change, not an enumeration record.

Malformed enums with duplicate names or numbers, or mismatched member/number counts, report
`EnumerationNotComparable`. Known changes in other comparable enums remain visible even when source is incomplete.
This report classifies differences between two models; it does not enforce the [release baseline](events.md#released-event-contracts).
The release baseline freezes resolved schemas by equality, so even an appended member or an explicit rename can
violate a baselined event revision despite no breaking numeric identity change here.

## Compare agent exposure separately

`ModelDifference.AgentExposure` is a typed `AgentExposureComparison` beside the legacy structural sections.
Its `Rows` are ordered by operation kind and resolved qualified name. An absent or explicitly empty agents block
exposes nothing. Adding exposure produces `Added` and comparison warning `PLAY0978`; removing it produces `Removed`.
A higher declared scope (`read < write`), changed exposed signature/referenced contract or admitted token-contract
loosening is `Widened` (`PLAY0979`). A lower scope alone is `Narrowed`. Description edits and alternate spelling of
the same resolved target are `Unchanged`.

Always inspect this aggregate's own `Coverage`, not the legacy `Complete` field. Parsing/validation errors on either
side make every available row `Unknown`, `Coverage` becomes `Incomplete` and `HasWidening` is null, even if no block
or row survived. Unknown is reserved for these errors, not warnings or executable-binding refusal. Without such
errors, an unresolved exposed contract is conservatively `Widened` for review, never Unknown or safely unchanged.
Never convert an incomplete result's null to a safe `false`.
Only personal tokens are supported; an unsupported-token attempt follows this error rule rather than comparing a
valid subset.

Executable-binding refusal is different: a preview `system`/`operation` can parse and validate cleanly, then refuse
binding with `PLAY0268`. Its authoring allowlist remains intact and is compared independently of binding. Such a
binding refusal without agents adds no exposure limit and preserves the existing legacy response bytes.

The typed aggregate is excluded from default CLI/library JSON serialization; legacy rows do not acquire new fields.
`NotCompared` adds exactly one `Agent exposure was not compared; use the agent-exposure view.` statement when either
side has a block (including empty), or either side has parsing/validation errors. Clean sources without agents add
nothing. MCP callers opt into [the agent-exposure view](mcp/reference.md#agent-exposure-difference) for bounded rows.
This structural report grants no authorization, issues no tokens and implements no runtime endpoint. See
[decision 0151](../../decisions/0151-explicit-agent-exposure-and-fail-closed-comparison.md).

## Group changes by slice

A tool that builds code slice by slice needs to know which slices to rebuild,
not every declaration that changed. `ModelDifference.Slices` is a
`SliceChangeSummary` that attributes every change to the slice that owns it:

```csharp
foreach (var slice in difference.Slices.Slices)
{
    Console.WriteLine($"{slice.Address}: {slice.Change}, dependents: {string.Join(", ", slice.DependentSlices)}");
}
```

For the example above the output is
`Projects.Registration.Register: Changed, dependents: `.

| Property | Meaning |
| --- | --- |
| `Slices` | One `SliceChange` per added, removed, renamed, moved or changed slice, ordered by address |
| `ModelWide` | Changed declarations no slice owns: modules, features, concepts, types, policies, personas and the like |
| `AffectedSlices` | Unchanged slices that depend on a changed slice or a changed model-wide declaration |

Each `SliceChange` carries:

- `Changes`, every `SliceChangeKind` that applies, and `Change`, the most
  significant of them in the order `Added`, `Removed`, `Renamed`, `Moved`,
  `Changed`. An added or removed slice reports only that. A renamed slice whose
  content also changed reports `Renamed` and `Changed`.
- `BeforeAddress` and `AfterAddress`, and `Address`, which is the address after
  the comparison when the slice still exists.
- `SemanticId`, the slice's persisted identity in Identity matching.
- `Declarations`, the changed declarations the slice owns, including the slice
  itself.
- `DependentSlices`, the other slices that consume something the slice
  declares, such as a projection from its event, on the side where the slice
  exists.

Renames and moves are only reported as such in Identity matching, where
persisted identities connect the two versions. Address matching reports a
renamed or moved slice as one removed and one added slice. A declaration that
only follows its renamed or moved slice does not count as a content change.

Model-wide changes can reach any slice. `AffectedSlices` lists the slices with
a direct reference to the changed declaration; a builder that needs certainty
rebuilds everything when `ModelWide` is not empty. Dependencies are direct,
not transitive: a slice that reads a view built from a changed event is not
listed unless it also references the changed slice.

## Coverage and limits

Inspect every `ComparisonSection` in `Sections`, not only the change lists.
`Gaps` contain typed `ComparisonGapKind` values and stable explanatory
statements. The kinds cover incomplete source, failed example resolution,
noncomparable assigned/indexed declarations, address-only unassigned
declarations/events/specifications, uncertain dependant references, omitted
identity comparison, declaration-level fallback and `EnumerationNotComparable` for malformed enum members or numbers.

`NotCompared` carries stable limit statements. Comparison does not analyze
behavior inside opaque code, open external implementation files, prove
transitive/runtime impact or execute specifications. Direct dependants retain
resolved, unresolved, ambiguous, incomplete and wrong-kind resolution states.
Properties use owner references; containers aggregate external references to
contained declarations while excluding references inside the container.

The MCP [semantic-diff views](mcp/reference.md#semantic-proposal-difference)
use the same structural comparison implementation and return the same per-slice
summary as `slices`. Their existing revision
checks, JSON layout and paging remain transport concerns; the library result
is typed and unpaged.
