---
title: "Cratis Screenplay CLI: install and validate a model"
description: "Install the Cratis Screenplay CLI, validate a saved .play application, read diagnostics and exit codes, and run model specifications separately."
---

# Compiler and CLI

The Cratis Screenplay CLI checks saved `.play` files before you pass a model to
another tool. It compiles a folder as one application and reports problems with
file locations and diagnostic codes. Use this check after editing in
[VS Code](vscode.md): a board can draw a model that the compiler rejects.
Source validation does not run the model's specifications; the
[specification runner](#run-the-models-specifications) is a separate command.

If you are building your own tooling, the compiler is also available as a
[.NET library](#use-the-compiler-as-a-library).

## Install the CLI

For a local installation, you need the .NET 10 SDK and the global .NET tool
directory on `PATH`. The CLI is a
[.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)
published to NuGet as `Cratis.Screenplay.Tool`. Install it globally:

```bash
dotnet tool install -g Cratis.Screenplay.Tool
```

This puts a `screenplay` command on your path. Update it later with `dotnet tool update -g Cratis.Screenplay.Tool`.

### Run it in Docker

The same CLI is published as the `cratis/screenplay` image, for machines without the .NET SDK and for CI. Mount the files you want to verify and pass the mounted path:

```bash
docker run --rm -v "$PWD/specifications:/work:ro" cratis/screenplay --warnaserror /work
```

The image also runs the [MCP server](mcp/index.md): `docker run -i --rm -v "$PWD/specifications:/model" cratis/screenplay mcp /model`.
Use its [`propose-move`](mcp/edit.md#move-a-slice-or-feature-to-another-parent) tool
for identity-preserving slice, feature and single-declaration moves across logical parents. This is an
MCP proposal, reviewed and applied through `apply`, not a CLI move command.

## Verify your files

Save your editor changes first: the CLI reads files from disk. Run it at the
model root to check every discovered `.play` file below that directory:

```bash
screenplay
```

Or pass the model folder explicitly:

```bash
screenplay path/to/screenplays
```

A directory is checked as **one application**. The compiler merges the files
before resolving references, so an event declared in one file can be produced
in another. Keep the whole model as the input when checking a change that uses
shared declarations. [Folders](folders.md) explains how the files fit together.

For a single-file model, pass the file:

```bash
screenplay path/to/invoicing.play
```

A file target includes that file and everything its [imports](imports.md) bring
in, but not unimported siblings. A declaration in a neighboring file will not
resolve unless it is part of those imports.

Each problem identifies the file, line, column, severity and diagnostic code,
then shows the source line and a caret at the location:

```text
nested/broken.play(3,5): error PLAY0028: Unknown slice type 'Wat' - expected StateChange, StateView, Automation or Translate
    3 |     slice Wat DoIt
      |     ^

2 file(s) compiled - 1 error(s), 0 warning(s)
```

Open that location in your editor, fix the declaration and rerun the same check.
The final summary counts compiled files, errors and warnings. Match diagnostics
by their stable code, not the message text; [Diagnostics](diagnostics.md) lists
every code. A run with no discovered files is not evidence that your model was
checked, even if it exits successfully.

The exit code is `0` when the check is clean, `1` for model defects, and `2` when
the check could not run, for example because a path, argument or scope is invalid.
Warnings do not fail the run by default. To make a warning fail validation, use:

```bash
screenplay path/to/invoicing.play --warnaserror
```

Ordinary native checks include [`PLAY0690`](diagnostics.md#reducer-instance-identity),
the conservative reducer identity warning. It needs neither executable binding
nor a completeness `--check` selection. As with other warnings, `--warnaserror`
makes it fail the check.

| Option | Effect |
|---|---|
| `--scope <Module>[.<Feature>[.<Slice>]]` | Report the named scope and declarations that directly reference it |
| `--check <name>[,<name>]\|all` | Select [completeness checks](completeness.md); repeat to combine selections. Unknown names exit `2` |
| `--open-questions report\|fail` | Report committed open model questions; `fail` exits `1` when any selected question remains. Unreadable notes exit `2` |
| `--warnaserror` | Warnings in the reported set fail the run - exit code `1` even with zero errors |
| `--no-color` | Never colorize output |
| `--format text\|json` | Text by default; JSON writes exactly one report to stdout |
| `--print-gate` | Print the effective gate and setting sources in text, then exit `0` without compiling |
| `--no-gate-file` | Ignore the authored gate for this run, including invalid or unreadable files |

Colors are enabled automatically on interactive terminals; disable them with `--no-color` or by setting the `NO_COLOR` environment variable.

For repeatable validation, [declare the gate once](#declare-the-validation-gate-once)
instead of repeating options in each script. To check modeled behavior after
source validation, [run the specifications](#run-the-models-specifications).
The runner reports unsupported capabilities separately from passing scenarios.

## Choose executable model semantics

An executable semantic model (ESM) is the bound model that execution and rendering
consumers use. Source validation and support for an ESM version are separate:
a consumer must explicitly accept the version your model requires.

Application-root `command reads protected` selects protected command-read admission (ESM v25), including read paths in requirements and produced-event mappings, required-absence rejection and decide/commit conflicts. Without it, legacy command reads remain refused. No CLI flag upgrades the model automatically; formatting/export preserve the preamble, and targets must separately admit the version. Invoicing demonstrates the authoring form with explicitly pinned binding refusals; the executable demonstration is `ProtectedCommandReadsCorpus.V25`.

Application-root `reaction batches atomic` selects ESM v22 for the composed application. No CLI flag or dependency update opts existing files in. Formatting and export preserve the declaration; omission retains legacy per-fact reaction semantics. Execution and rendering consumers must explicitly admit v22.

## Check generated references

The compiler accepts ESM v21 pattern/random/rule declarations and String-concept checksum validation. Source validity is separate from executable admission and reference-specification results. PLAY0691 is a hard generation-support budget error; PLAY0692 means empty conditioned support on an assessed UTC date. Neither is a successful unknown result. Use whole generated fixtures and `given clock` for pattern dates—never a seed or wall-clock assumption. See [patterns and checksums](generated-values/patterns.md) for the shop's invoice reference and numeric ticket, and [contexts](context.md) for ValueGeneration attachments. Providers must explicitly admit v21; parsing alone does not prove they can render or execute it.

## Admit and check released event revisions

Persist the workspace's `.screenplay/identities.json` before admission. At release time, run:

```bash
screenplay baseline admit path/to/model --release v1.4.0
screenplay baseline check path/to/model
```

Admission explicitly records every current and historical event revision in
`.screenplay/release-baseline.json`. Commit the file with the model. It adds new
revisions and keeps matching entries with their original release labels; it never
removes entries or re-admits a changed revision. Neither admission nor checking
changes source, executable model bytes, or source, semantic and catalog revisions.
Admission requires a model that binds, persisted identities and no pending recovery.
Writes use the shared `.screenplay/pending.json` recovery journal.

To baseline an earlier release, supply its canonical executable semantic model:

```bash
screenplay baseline admit path/to/model --release v1.3.0 --from-esm shipped-esm.json
```

The strict ESM reader requires the same application identity. From ESM v16 the
shipped model carries each renamed event's stored name (`storedName`), and admission
records that name. Earlier ESM versions do not carry rename pins, so the shipped
event name must match the current stored name. Either way, a shipped stored name
that differs from the current one refuses with `ShippedStoredNameMismatch`.

`baseline check` exits `0` when every admitted revision matches, `1` for
`BaselinedRevisionChanged` (with the first differing path) or
`BaselinedRevisionMissing`, and `2` when it cannot check, including an unreadable
baseline or a model that does not bind. A missing baseline is distinct from an
unreadable one. Malformed JSON, merge conflicts, another application's identity,
and a schema/hash mismatch never count as absence and are never repaired automatically.
Admission uses the same exit codes: changed overlapping revisions exit `1`,
invalid inputs or failed writes exit `2`.

The [published JSON Schema](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/release-baseline-v1.schema.json) describes the sidecar.
Composite types expand recursively. A cycle becomes `{ "kind": "ref", "ref": 1 }`
for the nearest enclosing composite, `ref: 2` for its parent, and so on. These
path-relative references exclude type names and identities, so renaming a recursive
type does not change its schema hash. References outside the enclosing composite
path make a baseline unreadable.

Checking is read-only and retains persisted identities even when source removes an
admitted event or generation. Such removals report `BaselinedRevisionMissing` with
exit `1`, without reconciling or writing the identity catalog.
See [released event contracts](events.md#released-event-contracts) for what is frozen.

## Declare the validation gate once

Stop repeating your checks in every script. Commit an authored `screenplay.json`
at the model root, beside `.screenplay/`, with the settings your model must satisfy:

```json
{
  "$schema": "https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/screenplay-v1.schema.json",
  "validation": {
    "checks": ["field-origins", "query-keys", "navigation"],
    "warningsAsErrors": true,
    "scope": "Workspaces.Tracking",
    "openQuestions": "fail"
  }
}
```

Every member is optional. Each `checks` item is one [check name](completeness.md),
diagnostic-code alias or `all`; comma-separated items are invalid. An empty list
selects no checks. `all` expands to every check supported by the running version,
so upgrading can strengthen your gate. An explicit list pins its membership.
Reports deduplicate checks and order them by the check catalog's declaration order.
`openQuestions` accepts `report` or `fail`; omitted means notes are not evaluated.
The [v1 JSON Schema](https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/screenplay-v1.schema.json) validates the authored shape in editors.

The tool starts its root search at the target folder, or a file target's parent.
It chooses the nearest folder at or above that start containing `screenplay.json`
or a `.screenplay/` directory. The search includes but never crosses the first
folder containing `.git` (a file or directory), and otherwise stops at the filesystem
root. Without a match, the start folder is the model root. Nested models keep their
own gates. Imports never influence this search, and finding a gate does not widen
the source files being compiled.

Each explicit command-line option replaces only its corresponding file setting.
Omitting `--warnaserror` does not override `warningsAsErrors: true`. If any `--check`
is given, the union of every command-line selection replaces the file's check list.
The file replaces the defaults: no checks, warnings do not fail, no scope and no
notes evaluation. A missing file preserves those defaults. `--no-gate-file` ignores
the file entirely, so you can relax a declared gate for one run.

Inspect the winning values without compiling, or obtain a machine-readable result:

```bash
screenplay path/to/model --print-gate
screenplay path/to/model --format json
screenplay path/to/model --no-gate-file --check navigation
```

`--print-gate` always prints text, even with `--format json`. The JSON report is one
JSON document on stdout, with `diagnostics`, `openQuestions`, `refusal`, `exitCode`
and `gate`. Each gate setting has `value` and `source` (`commandLine`, `file` or
`default`); the gate also carries `modelRoot` and `gateFilePath`. Checks are expanded
canonical names. Notes are null when not evaluated. A refused gate has `gate: null`
rather than a misleading partial effective gate; its refusal includes kind, path,
line, column and reason.

Malformed JSON, duplicate members, unknown keys, unknown check names and wrong
types refuse with `Invalid`. A directory, symlink/reparse point or unreadable file
at the gate path refuses with `Unreadable`. The tool never treats these as a missing
file or repairs them. Refusals exit `2` and report the location; clean checks exit
`0`, and model defects, warnings promoted by the gate or open questions in fail
mode exit `1`.

| File beside a model | Owner |
|---|---|
| `.play` files | Authored model source |
| `screenplay.json` | Authored configuration, committed by people; no tool writes it |
| `.screenplay/` | Tool-owned identities, notes, release baseline and recovery journal; use their documented operations |

Library hosts reference `Cratis.Screenplay.Validation`: call
`ValidationGateRoot.Resolve(target)`, then `ScreenplayValidationGate.Read(root)`
and `ValidationGateResolver.Resolve(read, commandLine, noGateFile)`. Pass a
`DeclaredValidationGate` containing only explicit command-line values; nullable
booleans distinguish omission from an explicit false. The read result distinguishes
absence from `ValidationGateFailureKind.Invalid` and `Unreadable`. Each host maps
those typed failures to its own invalid-input exit code.

## Report open model questions

`screenplay path/to/model --open-questions report` reads the committed
[model notes](model-notes.md) sidecar and emits host-only `info` findings with code
`model-note.open-question`. Use `--open-questions fail` to exit `1` while open
questions remain. Tasks and ordinary notes do not trigger this gate.

Keep the model root as the input. `--scope` selects descendant and ancestor notes,
including application notes. An unscoped report also includes orphaned questions.
The notes report uses persisted identity state when present, so anchors follow
renames. If `identities.json` is absent, the report says notes cannot be scoped,
keeps all open questions visible unscoped, and marks them orphaned with reason
`identity-state-absent`; `fail` still gates on that reported set.
An unreadable notes file exits `2` only when open questions are selected by the
flag or the gate file; otherwise notes are ignored. This is separate from compiler warnings and `--warnaserror`.

## Call these from your own tooling

### Export a workspace with a different document split

Reference `Cratis.Screenplay` for `ScreenplayWorkspace.Rehome` and
`ModelRootDocumentKeys.For`, both in `Cratis.Screenplay.Workspaces`. A host can
split its in-memory model into repository files without changing its semantic
IDs or event ContractIds. For example, given a workspace and the new files as
portable relative paths and UTF-8 bytes:

```csharp
using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

static byte[] IdentityStateFor(
    ScreenplayWorkspace workspace,
    ImmutableArray<(string Path, byte[] Bytes)> files)
{
    var documents = files.Select(file =>
    {
        var path = PortablePlayPath.Parse(file.Path);
        return WorkspaceDocument.Create(ModelRootDocumentKeys.For(path), path, file.Bytes);
    }).ToImmutableArray();
    var rehomed = workspace.Rehome(documents);
    return PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(rehomed));
}
```

Write those exact files and the returned bytes as `.screenplay/identities.json`
under the same root. Within the model root's file limits, strict
`ScreenplayModelRoot.Open` accepts this state. Re-homing itself is read-only.
It refuses source that does not compile, duplicate document keys or IDs,
semantic or event addresses that were added or removed, and any document set a
model root could not hold and discover again: paths that collide or differ only
in case (one spelling per file and folder), a document path that is also another
document's folder, folders discovery skips (`bin`, `obj`, `node_modules`, `.git`,
`.screenplay`, `.ai-work`), more than 16 folder levels, and more files or source
bytes than a model root admits.
Address refusals list `addedAddresses` or `retiredAddresses`; make model edits
through the ordinary revision-checked authoring workflow before re-homing.

Assignments for document keys supplied again survive; new keys get deterministic
bootstrap DocumentIds, and old document keys are dropped. Semantic and event
assignments keep their IDs and origins by address, including event revisions
unless the source advances an existing revision forward. Changed document
assignments change the catalog revision; changed documents or assignments
change the workspace revision, so old proposals become stale.
Attachments are not carried; re-apply them with `WithAttachmentContents`.
Discovery keys hash the portable path's NFC-normalized value. Existing persisted
keys remain authoritative, including keys created from non-NFC file names.

A repository that holds only part of the model, such as the features a host has
generated so far, uses `Rehome(documents, RehomeScope.Subset)`. Every declaration
the documents contain keeps its semantic ID and its event ContractId, revision and
origin; assignments for declarations the documents leave out are dropped, so a
strict open accepts the state. Re-home from the full workspace each time, not from
an earlier result: the left-out declarations then keep their IDs when they are
written later. The documents must compile on their own, declarations the workspace
does not know are still refused, and an event admitted to a release baseline that
the subset no longer contains is reported as `BaselinedRevisionMissing`.

Before overwriting an existing MCP-authored `identities.json`, compare its event
assignments with the host catalog. Different ContractIds make previously
baselined entries appear as `BaselinedRevisionMissing`; re-homing does not
reconcile identities assigned by another host.

### Run root-aware checks

Reference `Cratis.Screenplay.Mcp` to run the same root-aware checks without
installing or launching the standalone tool. In a console application, pass the
model root as the first argument:

```csharp
using Cratis.Screenplay.Mcp;

var root = args[0];
var baseline = ReleaseBaselineCommands.Check(root);
var questions = ModelNoteQuestions.Read(root);

foreach (var violation in baseline.Violations)
    Console.WriteLine($"{violation.Kind}: {violation.Event} revision {violation.Revision} at {violation.Path}");
foreach (var question in questions.Findings)
    Console.WriteLine($"{question.NoteId}: {question.Message}");
if (baseline.Reason is not null) Console.Error.WriteLine(baseline.Reason);
if (questions.Reason is not null) Console.Error.WriteLine(questions.Reason);

return Math.Max(baseline.ExitCode, questions.GetExitCode(failOnOpenQuestions: true));
```

`ReleaseBaselineCommands.Check(string root)` is read-only.
`ReleaseBaselineCommands.Admit(string root, string release, string? fromEsmPath = null)`
performs the deliberate, journaled admission step described above. Both return
`ReleaseBaselineOutcome`, carrying `ExitCode`, `Violations`, `FailureKind` and the
complete failure message in `Reason`. Successful admission also returns
`AdmittedEntryCount` (the total entries written) and `BaselineRevision`.

`ModelNoteQuestions.Read(string root, string? scope = null)` returns
`ModelNoteQuestionsOutcome` with `Findings`, `IdentityStatus`, `NotesStatus`,
`EffectiveScope`, `OrphanReason`, `FailureKind` and `Reason`.
`GetExitCode()` computes report mode; `GetExitCode(failOnOpenQuestions: true)`
computes fail mode. Missing identities clear `EffectiveScope` and preserve every
open question as an orphan. Unreadable notes or a read failure produce exit `2`
in either mode without repairing sidecars or changing the model.

### Open a model root with its persisted identities

A renderer or analyzer that needs the model itself, not just a verdict, opens the
root through `ScreenplayModelRoot` in `Cratis.Screenplay` (namespace
`Cratis.Screenplay.Workspaces`). It reads the root exactly as the MCP server and
`screenplay baseline check` do, so the workspace carries the identities in
`.screenplay/identities.json` instead of fresh ones:

```csharp
using Cratis.Screenplay.Workspaces;

var opened = ScreenplayModelRoot.OpenForReleaseBaselineCheck(args[0]);
if (!opened.Success)
{
    Console.Error.WriteLine($"{opened.FailureKind}: {opened.Reason}");
    return 2;
}

var check = ReleaseBaseline.Check(opened.Workspace, opened.ReleaseBaseline);
foreach (var violation in check.Violations)
    Console.WriteLine($"{violation.Kind}: {violation.Event} revision {violation.Revision}");
if (check.Reason is not null) Console.Error.WriteLine(check.Reason);

return check.Failure is not null ? 2 : check.Violations.IsEmpty ? 0 : 1;
```

Both `ScreenplayModelRoot.Open(string root)` and
`ScreenplayModelRoot.OpenForReleaseBaselineCheck(string root)` return
`ScreenplayModelRootOutcome` with `Success`, `Workspace`, `Identities`,
`ReleaseBaseline`, `FailureKind` and `Reason`. Opening never writes anything.
A pending recovery journal (`PendingOperation`), missing identities
(`PersistedIdentitiesRequired`), a corrupt identity file
(`IdentityStateConflict`), mapped files that are missing or unmapped
(`IdentityMappingConflict`) and an unreadable root (`UnreadableModelRoot`) come
back as a failure, not an exception. An unreadable baseline still opens; the
check reports it as `UnreadableReleaseBaseline`.

The two forms differ in what source they accept:

| Form | Removed declarations or mapped documents | Use it for |
|---|---|---|
| `Open` | Refused (`IdentityReconciliationRequired`, `IdentityMappingConflict`) | Authoring through the persisted identities |
| `OpenForReleaseBaselineCheck` | Kept in the catalog, so a check reports them as `BaselinedRevisionMissing` | Release-baseline checks, and rendering with baseline warnings |

When `Open` succeeds, both forms produce the same model revision. To render with
release-baseline warnings, render `opened.Workspace.Compilation` from
`OpenForReleaseBaselineCheck` and assess that same workspace. The assessment
then matches the rendered revision. Never author with, or save, the workspace from
that form: its catalog keeps identities for declarations that no longer exist.

To read only the identity file, for example one passed as a catalog path, use
`PersistedIdentityStateSerializer.Deserialize(bytes)`. It returns the application
name, the `IdentityCatalog` and the document mappings, and throws
`InvalidPersistedIdentityState` for corrupt, noncanonical or unsupported content.
Do not parse the file's JSON yourself.

## Report processing purposes

`screenplay report processing [<file.play|folder>] --format json|markdown|csv` derives a controller's [record of processing](purposes.md#record-of-processing) from error-free source. The default format is Markdown and the default path is the current directory. Supply `--controller-name` and `--controller-contact` explicitly; the model does not infer them. The report records declarations and limited structural derivations, not legal advice or verified runtime protection. Source errors exit `1`; bad arguments, unavailable paths and empty model directories exit `2`. A model with no purposes yields zero rows.

## List the environment keys a model needs

`screenplay environment [<file.play|folder>] [--format text|json]` lists the keys a model needs from the [environment](environments.md) it runs in — each authentication provider's `clientId` and `clientSecret`, the mail settings of onboarding — and marks the ones that must be secrets. The default path is the current directory and the default format is text, one key per line. Source errors exit `1` and list nothing; bad arguments, unavailable paths and empty model directories exit `2`. A model that needs nothing prints a single line saying so.

## Migrate files written for an earlier version

`screenplay migrate <file.play|folder> --from <version> [--to <version>] [--dry-run] [--report <file>]` rewrites files written for an earlier Screenplay version - for example `--from 5` for the forms Screenplay 6 split into slots and outlets - and compiles the result. `--to` defaults to the version the compiler reads. It exits `0` when every file migrated, `2` when a file needs review and `1` when it could not run. See [Migrate a model to a new version](migration.md).

## Check structural completeness

Select additional warnings and information with `--check data-bindings,input-surfaces,field-origins,query-keys,event-consumers,navigation,purposes`, `--check enum-pinning` (or `PLAY0671`) or `--check all`. Combine with `--warnaserror` to gate on findings, and with `--scope` to limit the reported set. Checks are not part of ordinary compilation and run only when the whole application has no source errors. Otherwise output reports `completeness checks skipped: the model has N error(s)`. See [Completeness checks](completeness.md) for rules and exemptions.

## Check one part of the application

Keep the application root as the input and name the part you changed:

```bash
screenplay path/to/screenplays --scope Billing.Invoices.SendInvoice --warnaserror
```

`--scope` uses case-sensitive, dotted module, feature and slice addresses, as MCP navigation does. Descendants are included; a partial name does not match another module. Nested features use their full dotted address. An unknown or ambiguous scope is a usage error, not an empty successful check. Selection follows the module, feature and slice hierarchy; same-named types, concepts and event sources are not descendants.

The compiler still resolves references across the whole application. Only diagnostics located in the selected declarations, plus declarations that directly reference them, are reported. A dependent declaration is included in full, not its whole slice; dependents of dependents are not included. The exit code and `--warnaserror` apply only to that reported set. The final summary labels its counts as **in scope**; a separate **Whole application** line always shows the full error and warning counts and how many diagnostics are outside the reported set. That line is red when the whole check fails, even if the scoped exit code is `0`.

Output includes the scope's declaration count, the additional direct-dependent declaration count, the diagnostic count, and **affected scopes** containing those dependents. `<application>` means an application-level declaration. Check the listed scopes or the whole root when you need wider assurance.

Impact uses the same explicit source-reference index as MCP `dependencies`. It does not inspect inline code, property paths, imports or expression identifiers, and ambiguous candidates are included conservatively. Unresolved references matching a name in scope are included conservatively as direct dependents. **Unresolved event consumers (cannot be attributed to a scope)** separately reports the count and scopes of unresolved event references outside the reported declarations. Their former targets cannot be proved after a rename or removal, so they do not change **affected scopes**, scoped diagnostic counts or the scoped exit code; their errors remain visible in the **Whole application** line. **Possibly affected** counts only the remaining unresolved references outside the reported declarations, excluding both direct dependents and the unresolved-event category. Diagnostics outside declaration ranges fall back to the file's import placement or its selected slice; diagnostics without a path or valid line are application-wide and included in every scoped result. A clean scoped check is not proof that the whole application is valid or executable. No watch or incremental cache is provided by this option.

## Run the model's specifications

Run scenarios without rendering an application or starting services:

```bash
screenplay test path/to/screenplays
screenplay test path/to/invoicing.play --filter Billing.Invoices.Send.SendingAnInvoice --format json
```

`PATH` defaults to the current directory. The run includes the specifications written inside [state machines](statemachines.md) and [workflows](workflows.md), addressed `<Name>.<Specification>`, and the [graph](graphs.md) specifications of a slice, addressed `<Slice>.<Specification>`. A folder is one application; a file includes only that file and its imports, not unimported siblings. `--filter` takes one exact, case-sensitive dotted specification address (module, nested features, slice, specification). A table address selects every named case; a derived `<Specification>_<Case>` address selects one case. Unknown selections are usage errors, not empty passes. Without a filter, every discovered specification is selected. `--format` defaults to `text`; `json` writes one camel-case JSON report to stdout.

When `PATH` is a folder holding `.screenplay/identities.json`, the printed semantic ids are the persisted identities the MCP server uses, so they survive renames made through MCP or `screenplay rename`. A single file, a folder without identities, and a folder whose sources no longer match its identity file (a hand edit or file rename) run with ids derived from addresses instead; a specification run never refuses because of identity state. Any printed semantic id is also accepted by `--filter`.

The reference evaluator runs deterministically in memory. It never contacts external services or runs attached code. Clock, trigger and capture scenarios use the [specification evaluator](specifications.md)'s existing semantics. Opaque reducers and other missing capabilities produce `unsupported`, with a capability and reason, rather than a pass. Failures include expected and actual facts, read models, command responses and query results; example-based failures also include effective fixture provenance.

A projection the reference evaluator cannot execute does not stop unrelated specifications. A specification is `unsupported` if its given, appended, produced or reaction-cascade events reach that projection, or if it supplies, reads, queries or asserts the projection's read model (including an absence assertion). Application-level capability issues still prevent every selected specification from running. `planIssues` lists both application-level and deferred slice-level issues, even when the selected specifications pass.

The report includes `sourceRevision`, `outcome`, `discovered`, `selected`, `executed`, `passed`, `failed`, `unsupported`, `diagnostics`, `planIssues` and `results`. Counts cover the complete selection; `executed` excludes unsupported scenarios. Each result gives its `address`, `semanticId`, `outcome`, `executionOutcome`, comparison `failures`, and nullable `capability` and `reason`. A model that does not bind reports `unbound` and executable diagnostics, with no scenario results. A bound model with no specifications reports zero discovered; that is not evidence of scenario coverage.

| Exit code | Meaning |
|---|---|
| `0` | Every selected specification passed |
| `1` | At least one selected specification failed, even if others were unsupported |
| `2` | Invalid arguments, selection or input, or the command could not run |
| `3` | The model did not bind, or nothing failed but reference execution was unsupported |

## Export the executable model

Write the canonical executable semantic model (ESM) of a model, for a builder, a script or a CI step:

```bash
screenplay export path/to/screenplays --output model.esm.json
screenplay export path/to/screenplays --scope Billing.Invoices.Send
```

The bytes are exactly what `SemanticModelSerializer.Serialize` writes for the same model, including `schemaVersion` and `revision`. Validate them, or generate types for them, with the [JSON Schema](executable-model-schema.md) published for their version. `PATH` defaults to the current directory, and `--format esm` is the only format. Without `--output` the JSON goes to standard output; with it the file gets the exact bytes, and an identical file is left untouched so incremental builds are not invalidated.

`--scope <Module>[.<Feature>...][.<Slice>]` keeps one module, feature or slice and drops every other module, feature and slice. Application-wide declarations (concepts, types, policies, triggers, event sources, lifecycles and graphs) stay, so references from the selected slices still resolve. The excerpt's `revision` identifies the excerpt, not the whole model. From .NET, use `ExecutableSemanticModelScope.Select(model, scope)`.

| Exit code | Meaning |
|---|---|
| `0` | The model was written |
| `1` | The model has no executable model; its errors are listed on standard error |
| `2` | Invalid arguments or scope, or the model could not be read |

## Compare two model versions

Compare two versions of a model, each a `.play` file or a model folder, to see what changed between them:

```bash
screenplay diff before/screenplays after/screenplays
screenplay diff before/screenplays after/screenplays --format json
```

The command runs `ModelComparison.Compare`, the comparison the library offers, and prints added, removed, renamed and moved declarations, member changes, event contract changes (marked `[contract-breaking]` where stored events would no longer read), enumeration changes, specification outcome changes, identity changes and the direct dependants of each change. It ends with the [changes grouped by slice](model-comparison.md#group-changes-by-slice): each added, removed, renamed, moved or changed slice with its dependent slices, the model-wide changes and the unchanged slices they affect, so a builder knows which slices to rebuild. To compare two commits, check each out into its own folder with `git worktree add`.

Declarations are matched by persisted identity when both folders hold `.screenplay/identities.json` for the same application, so a rename or a move reads as one. Otherwise they are matched by address, and a rename reads as a removal plus an addition; the report says which matching it used and lists what it could not compare.

`--format json` writes one object: `before` and `after` (path, source revision, whether persisted identities were used, model revision) and `difference`, the `ModelDifference` with camelCase names and enumerations as strings; `difference.slices` holds the per-slice summary. The command exits `0` when it compared the versions and `2` when it could not run, including when the two folders hold identities for different applications.

## Trace a value's lineage

`screenplay lineage` prints where one value comes from and where it goes: the same hop tree the MCP `trace-lineage` tool pages, all at once. The address names a command, event or read-model property (the owner's address and the member name) or a screen field (the screen's or form's address and the property it binds).

```bash
screenplay lineage Lending.Catalog.AddBook.AddBook.title Samples/Library
```

```text
Lineage of Lending.Catalog.AddBook.AddBook.title (Property), both
command-property Lending.Catalog.AddBook.AddBook.title 'title BookTitle' (library.play:78:9)
  -> production-mapping Lending.Catalog.AddBook.AddBook 'title = title' (library.play:90:11)
    -> event-property Lending.Catalog.AddBook.BookAdded.title 'title BookTitle' (library.play:95:9)
      -> projection-mapping Lending.Catalog.BookCatalog.BookCatalog 'title = title' (library.play:164:11)
        -> read-model-property Lending.Catalog.BookCatalog.CatalogEntry.title 'title BookTitle' (library.play:150:9)
          -> query Lending.Catalog.BookCatalog.AllBooks 'query AllBooks' (library.play:158:36)
          -> query Lending.Catalog.BookCatalog.BookById 'query BookById' (library.play:154:25)
          -> screen-display Lending.Catalog.BookCatalog.BookCatalog 'column title' (library.play:176:11)
  <- screen-input Lending.AddBookInput 'field title' (library.play:57:5)
9 hop(s), complete.
```

`->` is where the value goes and `<-` is where it comes from. A break (a missing mapping, an ambiguous name, code the trace cannot read) is shown in brackets with its reason. `--direction upstream|downstream|both` narrows the trace and `--format json` prints every hop with the fields described in [field lineage](mcp/reference.md#field-lineage). The command exits 0 for a complete trace, 1 when it has breaks and 2 when it cannot run, for example when the address names nothing.

## Rename a declaration and keep its identity

Rename a module, feature, slice or declaration through the same proposal and apply the MCP server uses:

```bash
screenplay rename Billing.Invoices.Send.InvoiceSent InvoiceDispatched path/to/screenplays
screenplay rename Billing.Invoices.Send.Invoice Bill --kind Command
```

The address is the dotted path to the declaration, and the folder defaults to the current directory. Pass `--kind` (`Command`, `EventContract`, `ReadModel`, ...) when the address names more than one declaration. The rename repairs references across files, keeps the declaration's semantic identity, pins a renamed event's stored type name with `id`, and writes `.screenplay/identities.json` exactly as an MCP apply does, creating it on the first rename. It exits `0` when renamed, `1` when the rename was refused and `2` when it could not run.

`test`, `export`, `diff` and `rename` open a folder the way the MCP server does: with `.screenplay/identities.json` when it is present, so the CLI and MCP report the same identities for the same model. `export` and `diff` refuse a folder whose sources no longer match its identity file (`IdentityReconciliationRequired`, `IdentityMappingConflict`) rather than inventing new identities; restore the previous names, then rename through `screenplay rename` or MCP.

## Export the language and tool contract

To check tooling or AI guidance against the installed Screenplay release, export its machine-readable contract:

```bash
screenplay contract
screenplay contract --output screenplay-contract.json
```

Without `--output`, the command writes one JSON document to stdout. With it, the command writes UTF-8 JSON to the given file (overwriting an existing file); its parent directory must exist. No model directory, MCP connection or external service is needed. Exit code `0` means the document was written; `2` means invalid arguments, an unclassified contract fact or a write failure.

The document has `schemaVersion: 1` and contains:

- `keywords`, derived from the compiled parser dispatch and reserved-word catalog, and `topLevelConstructs`, accepted at the application document root.
- `constructs`, including container and slice declarations, with `parseStatus: accepted`, bound `probes`, and an `admission` entry for each supported ESM schema version. Each probe publishes its source, observed `minimumSemanticVersion`, primary disposition `diagnostic`, every raised `diagnostics` code and severity, and admission per schema. `admitted` means the baseline probe has backend executable meaning; `refused` also covers authoring/UI metadata not carried into the backend model. `conditional` means another published probe is refused; its `condition` lists zero-based indexes into `probes`. These examples are evidence, not an exhaustive inventory of every grammar combination. `diagnostic` is the actual refusal/disposition PLAY code, or `null` where none is raised, including version-only refusals; `issue` is the tracking URL when the binder names one. Parsing alone does not imply execution.
- `diagnostics`: every C# `DiagnosticCodes` entry, its catalog-summary `title`, primary `severity`, all emitted `severities`, and `reserved` and `retired` flags. The primary severity is the highest possible severity; some codes vary by context. Reserved but unretired entries remain listed.
- `mcpTools`: every tool, including `run-specifications` and the optional board tool, with `requiredParameters`, `optionalParameters` and its complete `inputSchema`. Conditional requirements remain in the schema; an optional parameter may be required for a particular view.
- `cliCommands`, from the definitions used by the tool dispatcher and argument readers, including the default check command (empty name), usage, options and information-command aliases. MCP includes rootless startup and the explicit `--create-root` alternative.
- `esmVersions`, with schema versions and supported language/semantic version pairs. Construct admission describes the portable backend model, not renderer or reference-evaluator capability.

The release asset is named `screenplay-contract.json`; the same generated document is included at the root of the `Cratis.Screenplay.Tool` NuGet package. Consumers should check the schema version before comparing facts.

For a library call, reference `Cratis.Screenplay.Contracts` and use `Cratis.Screenplay.Contracts.ScreenplayContract.Write(TextWriter)` or `Serialize()`. Neither entry point opens an MCP server or reads a model from disk.

### Regenerate the contract golden

From a Screenplay source checkout, regenerate the checked-in golden after an intentional language or tool change:

```bash
dotnet run --project Source/DotNET/Tool -- contract --output Source/DotNET/Screenplay.Contracts/Golden/screenplay-contract.json
dotnet test Source/DotNET/Screenplay.Contracts/Screenplay.Contracts.csproj --configuration Debug
```

Review the JSON diff. Ordinary specs compare the generated document with the golden and never rewrite it. Every admission probe uses the same workspace compilation path as a real model, including source validation and timeline analysis before semantic binding. Generation fails with the probe's name if it raises an error other than an unsupported, deferred or report-only semantic disposition; malformed examples cannot become published admission refusals by updating the golden. A new conditional rule needs an example and condition in the admission probe table.

## Use the compiler as a library

The `Cratis.Screenplay` NuGet package provides parsing, the syntax tree, diagnostics, file discovery and formatting:

```bash
dotnet add package Cratis.Screenplay
```

Compiling source text gives you a syntax tree and any diagnostics:

```csharp
using Cratis.Screenplay;

var compiler = new ScreenplayCompiler();
var result = compiler.Compile(source);

if (result.Success)
{
    var application = result.Value!;
}
```

To turn the syntax tree into your own representation, derive from `ScreenplaySyntaxWalker` and override the node kinds you care about - the base class walks everything else, so a construct you never asked about cannot break you. The root visitor interfaces (`IApplicationSyntaxVisitor<T>`, `IProjectionSyntaxVisitor<T>`, ...) are the alternative when your consumer is one transformation of the whole document; pass one to `Compile` and the compiler drives it once parsing succeeds. See [Visitors and traversal](visitors.md) for both, and [Sub-language Pluggability](sub-languages.md) for how the language itself is layered.

Compiling every `.play` file beneath a directory as one application - what the CLI does for a directory - is a single call:

```csharp
using Cratis.Screenplay.Files;

var compilation = new PlayFileCompiler().CompileFolder(rootDirectory);
var application = compilation.Result.Value!;
```

A single file is a single call too:

```csharp
var compilation = new PlayFileCompiler().CompileFile(path);
```

`CompileFile` compiles that one document. `CompileApplication(path)` compiles the application the file is the root of - the file and everything its [imports](imports.md) bring in - which is what the CLI does:

```csharp
var compilation = new PlayFileCompiler().CompileApplication("application.play");
```

`CompileIn(rootDirectory)` is the third option: it compiles every discovered file as a document in its own right and hands back one result per file. Reach for it only when the files genuinely are separate documents that happen to share a directory - see [Folders](folders.md) for why a folder is normally one application.

### Validate a scope from .NET

To use the same scoped validation as `screenplay --scope` and MCP `diagnostics`, also reference the `Cratis.Screenplay.Mcp` NuGet package:

```bash
dotnet add package Cratis.Screenplay.Mcp
```

Pass all application sources, keyed by application-relative path (every key is a compilation root), the case-sensitive scope address, and the [completeness checks](completeness.md) you want. This excerpt assumes `sources` is an `IReadOnlyDictionary<string, string>` containing the whole application, including imported documents:

```csharp
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;

if (!ScopedDiagnostics.TryValidate(sources, "Billing.Invoices.SendInvoice", CompletenessChecks.None, out var result, out var error))
{
    Console.Error.WriteLine($"{error.Kind}: {error.Message}");
    return;
}

var scopedErrors = result.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
Console.WriteLine($"Scope: {scopedErrors} error(s); whole application: {result.WholeApplicationErrorCount} error(s), {result.WholeApplicationWarningCount} warning(s)");
```

`TryValidate` returns `true` when the scope resolves uniquely, even if the model has errors. On `false`, `result` is null and `error` carries `UnknownScope` or `AmbiguousScope` plus its message; no exception is thrown for either outcome.

The immutable `ScopedDiagnosticResult` contains `Scope`, `Diagnostics`, `DeclarationCount`, `DependentDeclarationCount`, `AffectedScopes`, `UnresolvedEventConsumers` (`ReferenceCount` and `Scopes`), `PossiblyAffectedReferenceCount`, `DependencyCoverage`, `WholeApplicationErrorCount` and `WholeApplicationWarningCount`. An empty scope address in an affected or unresolved-consumer list denotes an application-level declaration. Whole-application counts include requested completeness findings, which run only when the whole application has no source errors. Use `CompletenessChecks.None` to omit them.

You can pass a path instead of `sources`: the `TryValidate(string path, ...)` overload follows the tool's file and folder semantics, including imports and file encodings, and returns `InvalidPath` for a missing path or a non-`.play` file, or `UnreadablePath` for I/O and permission failures, rather than throwing.

The [scoped selection rules above](#check-one-part-of-the-application) apply unchanged: direct dependents only, uncertainty reported separately, and a clean scoped result is not a whole-application or executable-model verdict. Both overloads start a fresh analysis for each call; the dictionary overload reads only the supplied sources.

To go the other way - turn a syntax tree back into `.play` text, or generate Screenplay from a model - see [Printing and generating](printing.md).
