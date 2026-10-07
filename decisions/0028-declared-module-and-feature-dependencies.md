---
id: 0028
title: Let modules and features declare what they depend on, checked against the inferred graph
status: proposed
stage: none
class: product
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Dependencies/**
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Since [#414](https://github.com/Cratis/Screenplay/issues/414) ([PR #455](https://github.com/Cratis/Screenplay/pull/455)) Screenplay infers how slices, features, modules and other bounded contexts depend on each other. The graph is read from references inside slices, so it is always true to the model: if `Payroll` projects an event that `Timesheets` produces, the graph says Payroll depends on Timesheets. The MCP `dependency-graph` tool shows edges by kind (`usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks`, `shows`, `verifiedWith`, `outsideTheModel`), implied container edges at every level including mixed ones, cycle groups and a suggested story order ([MCP reference](../Documentation/screenplay/mcp/reference.md)). The timeline check reports backward event flow (`PLAY0516`) and mutually dependent sibling groups (`PLAY0517`) as information ([timeline diagnostics](../Documentation/screenplay/imports.md#timeline-diagnostics)).

What the inferred graph cannot say is what the authors *meant*. [#416](https://github.com/Cratis/Screenplay/issues/416) proposes letting a module or feature state its dependencies. A declaration adds three things the graph does not:

- **Stated intent.** "Payroll depends on Timesheets" becomes part of the model a reader sees, not something reconstructed from slices.
- **Drift detection.** When a new slice makes Payroll reach into Engagements, the model says so at the point of change instead of waiting for someone to inspect the graph.
- **A simple architectural rule.** Timesheets should never depend on Payroll. A declaration gives the compiler a direction to check.

The cost is a new statement in the language, a second list to keep in sync with the slices, extra warnings for teams that opt in, and work in both compilers, the printer, the typed AST, MCP authoring and rename. The graph already answers "what depends on what" without any of that, so the question is whether the intent and the checks are worth it.

Existing rules this record has to fit:

- `uses` is taken on modules and features for behaviors ([grammar](../Documentation/screenplay/grammar.md)), and on operations for systems.
- Notation principles: a construct reads as a statement about the system, prefers qualified operands to new sub-clause keywords and indentation to parentheses ([#81](https://github.com/Cratis/Screenplay/issues/81)); dotted paths are a retained convention ([#350](https://github.com/Cratis/Screenplay/issues/350)).
- Names resolve inside out, innermost first, and a qualified name may use any unambiguous trailing part of the scope path; ties are warnings that name the candidates ([screens](../Documentation/screenplay/screens.md)).
- Module and feature fragments in a folder model merge by name; `authorize` and `uses` accumulate per owner with duplicates kept once and warned ([folders](../Documentation/screenplay/folders.md)).
- Declarations are order-independent ([0023](0023-command-production-model.md)). Authoring metadata such as event `description` or personas adds no ESM bytes ([events](../Documentation/screenplay/events.md#authoring-metadata), [personas](../Documentation/screenplay/personas.md)).
- External event origin is not implemented yet ([0009](0009-external-event-origin-and-translation-slices.md)), so the graph knows other bounded contexts only through contract imports.

## Decision

*Proposed, not decided.* Adopt option B. A module or a feature may state `depends on <name>` once per target, where the target is a module or a feature. The statement is authoring metadata: it adds no ESM bytes and never changes execution. Nothing is checked for a container that declares nothing. Once a container declares at least one dependency, the compiler compares its declarations with the inferred graph. An undeclared inferred dependency is a warning, a declared dependency nothing uses is information, and an inferred dependency that runs against a declaration is a warning. The details below are part of the proposal.

### Spelling

```screenplay
module Payroll
  description "Pays people for approved time"
  depends on Timesheets

  feature Handover
    depends on Timesheets.Approval
    depends on Runs
```

In the TimeTracking sample, `Payroll.Handover` reacts to `TimesheetApproved` from `Timesheets.Approval` and invokes `AddHoursToPayroll` in its sibling `Payroll.Runs`.

- One target per line. A container with several dependencies repeats the statement. This keeps every target on its own line for diagnostics, rename, merge and diffs, and avoids a list syntax.
- The target is a dotted name, resolved with the existing rules, restricted to modules and features:
  - A bare name resolves inside out: the declaring container's siblings first, then its ancestors' siblings, then modules. `depends on Runs` inside `Payroll.Handover` is the sibling feature `Payroll.Runs`.
  - A dotted name matches any unambiguous trailing part of a container's full address. `Timesheets.Approval` is the `Approval` feature in `Timesheets`.
  - Two equally near matches are an ambiguity warning naming the candidates, as for screens. A sibling feature and a module with the same name resolve to the sibling (innermost wins); to name the module, rename one of them.
- `depends`, like other body words, is contextual and only meaningful at the start of a module or feature body line.

### Checking

- **Who is checked.** Each container that declares at least one dependency, on its own. Declaring on `Payroll.Runs` does not opt in `Payroll`, and the reverse.
- **Which edges.** The edges leaving the container: a consumer slice inside it and a producer slice outside it. Edges inside the container are not its business.
- **Satisfaction.** An edge is declared when a declaration on the checked container, or on a container between it and the consumer slice, names a target that contains the producer slice. A module target is satisfied by an edge into any slice in that module; a feature target only by an edge into a slice in that feature or its sub-features. A module can therefore declare coarsely while one of its features declares precisely.
- **Edge kinds that count.** `usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks` and `shows`. A declaration states coupling, not story order, and a screen showing another module's query or invoking its command is coupling. `verifiedWith` is excluded: specifications set up facts from elsewhere as a matter of course. `outsideTheModel` is excluded: it points at a bounded context, which this record does not let a module name (see option D). Ambiguous references satisfy a declaration when any alternative lies in the target and never raise an undeclared finding; unresolved references never become edges, as today.
- **Findings.**
  - Undeclared inferred dependency: **warning**, once per checked container and producer container at the declared granularity, with the edge's evidence.
  - Declared dependency nothing uses: **information**, on the declaration. A new module often declares ahead of the slices that use it.
  - Dependency against a declared direction: **warning**, when container X declares `depends on Y` and a slice in Y depends on a slice in X. It is reported whether or not Y opted in, because X's declaration already says which way the dependency runs.
  - Invalid target: **warning** for a target that is the container itself, one of its ancestors or one of its descendants, or that does not resolve. These cannot be checked; the inferred graph excludes the same pairs.
- **Cycles.** A declaration never makes a cycle acceptable. `PLAY0516` and `PLAY0517` are unaffected by declarations. Two containers that declare each other are reported as a warning, because the declarations themselves describe a cycle.
- **Codes.** Diagnostic codes are allocated by the implementing change, as usual.

### Representation

- **ESM.** Excluded. Executable bytes, revisions and identities do not change, as with personas and event authoring metadata.
- **Syntax and AST.** Module and feature syntax gain an ordered list of declared dependencies, each with its target text and location. Syntax JSON changes are additive. MCP and C# workspaces get typed add and remove edits, and `declaration-details` shows the declarations. `dependency-graph` reports, per edge, whether it is declared, undeclared or against a declaration, and lists declarations nothing uses.
- **Printing.** Printed in authored order, directly after `description`, one line each. Printing and reparsing gives the same syntax.
- **Folder models.** Declarations on the same module or feature accumulate across files, in file-path order. A repeated identical declaration is kept once with a warning, like a repeated `authorize`. Layout expansion writes them only in the owner's own file and strips them from restated headers, like `description`.
- **Rename.** A module or feature rename repairs `depends on` targets as proven typed references. If a target cannot be proven, the rename refuses rather than leaving a stale target, as the planner already does for other references.
- **Both compilers.** The C# and TypeScript compilers report the same findings. The TypeScript side depends on #414's TypeScript twin of the graph.

## Options considered

- **A. Inferred only, no syntax (status quo after #414).** Not recommended as the end state, but it is the default and costs nothing to keep. The graph is always correct, but intent lives in prose, and nobody is told when a new slice adds a dependency.
- **B. Explicit `depends on` on modules and features, opt-in, checked against the inferred graph (recommended).** Proposed in #416 and recommended by the earlier ordering and dependencies research (option D3). Intent becomes part of the model while the graph stays the evidence, and teams that declare nothing see no change. Prior art: Gradle and Maven "used undeclared / declared unused", Nx module boundaries, ArchUnit and dependency-cruiser rules over an inferred graph. A narrower variant, module level only, is what the research recommended first. It is not taken because the graph already resolves feature-to-feature edges across modules, and #416's examples need feature targets. Slice-level declarations are not proposed at all: they would repeat the references the slice already contains.
- **C. Explicit only, or required everywhere.** Not taken. Every module would have to list what its slices already show, the list would drift, and requiring it would turn the compiler's own knowledge into busywork. This is the JPMS `requires` end of the range, which fits code packaging, not a business model.
- **D. A context map at `domain` level** (upstream/downstream, anticorruption layer, as in DDD context maps). Deferred, not rejected. It needs [0009](0009-external-event-origin-and-translation-slices.md)'s event origin to know where outside events come from. Until then, relationship kinds can be inferred and shown as labels: an imported event consumed directly reads as conformist, one consumed through a `Translate` slice as translated.
- **Spelling alternatives.**
  - `depends on feature Approval in Timesheets` is not taken: it adds sub-clause keywords where a qualified operand does the job, which #81 argues against.
  - An indented block listing targets under `depends on` is not taken: one line per target merges, diffs and reports better and needs no new block grammar.
  - `uses` is taken; `requires` reads as a validation rule; `after` confuses dependency with story order.
- **Negative rules** (`never depends on`) and layering rules are not proposed. A declared direction already covers the common case. They can be added later without changing anything decided here.
- **Ordering kinds only for satisfaction** (`usesFactsFrom`, `reactsTo`, `decidesFrom`, the kinds the graph uses for cycles and order). Not recommended: an `asks` or `shows` edge would then be coupling nobody has to declare. It is the alternative to choose if the extra warnings prove noisy.

## Default if unanswered

Option A stays: the inferred graph, the MCP view and the information-level timeline diagnostics, with no syntax. Nothing has to be migrated, and adding B later is purely additive. The cost is that intent stays in descriptions and conversations, drift is noticed only by someone reading the graph, and there is no compiler-checked architectural rule. #387 and #388 work from the inferred graph either way.

## Timeline and scope

The decision holds until a context map at `domain` level is designed, which can extend it. Implementation should wait for #414's TypeScript twin, so that both compilers check the same way, and for the decider's verdict on the questions below.

In scope: the `depends on` statement on modules and features, its resolution, the checks and their severities, ESM exclusion, printing, typed AST and MCP support, folder merging and rename.

Out of scope: slice-level declarations, contexts and the context map (option D), negative and layering rules, deriving or applying a story order from declarations, board visualization of declared edges (#415), and any change to `PLAY0516`/`PLAY0517`.

## Verification

**Done when** a module or feature can declare `depends on <module>` or `depends on <module>.<feature>`, both compilers report the three findings with the same codes and locations on a shared vector set, the declarations add no ESM bytes, round-trip through the printer, merge across files and follow a rename, and a container without declarations reports nothing new.

**Verify by**:

- In TimeTracking, declaring `depends on Timesheets.Approval` and `depends on Runs` on `Payroll.Handover` raises no undeclared finding for Handover. Removing the first while keeping the second raises the undeclared warning, with evidence that resolves to the `QueueApprovedHours` reaction trigger.
- Declaring `depends on Payroll` on `Timesheets` reports the reverse-direction warning.
- The executable model bytes and revision of every sample are unchanged with and without the declarations.
- A module rename through `propose-rename` updates every `depends on` target that names it.

## Consequences

- **Easier:** reading a module's place in the system from its header, catching accidental coupling when it is introduced, and stating "this may not depend on that" without a separate tool.
- **Harder:** teams that opt in maintain a list next to the slices, and `--warnaserror` builds fail on undeclared dependencies. That is the point of opting in, but it is real friction. Every tool that edits modules and features (printer, AST, MCP, rename, layout expansion) gains one more element to carry.
- **Forecloses:** using `depends on` with a different meaning, such as ordering or runtime deployment. It also commits the module and feature header to authoring metadata that tooling must keep in sync, which is why the decision is costly to reverse once models use it.

## Questions for the decider

1. Do we want explicit declarations at all now (B), or stay with the inferred graph (A) until teams ask for them?
2. Module and feature level (recommended), or module level only to start?
3. Should `asks` and `shows` count as dependencies (recommended), or only the ordering kinds?
4. Are the severities right: undeclared **warning**, unused **information**, against a declared direction **warning**? A warning fails `--warnaserror` builds for teams that opt in.
5. Is the spelling `depends on Timesheets`, `depends on Timesheets.Approval`, `depends on Runs`, one per line, acceptable, and is "sibling wins over a module of the same name" acceptable?
