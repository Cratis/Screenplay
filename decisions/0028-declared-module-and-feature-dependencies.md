---
id: 0028
title: Let modules and features declare what they depend on, checked against the inferred graph
status: accepted
stage: none
class: product
reversibility: costly
decided: 2026-10-07
decider: Sindre Alstad Wilting
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

Since [#414](https://github.com/Cratis/Screenplay/issues/414) ([PR #455](https://github.com/Cratis/Screenplay/pull/455)) Screenplay infers how slices, features, modules and other bounded contexts depend on each other. The graph is read from the explicit references inside slices that the compiler can resolve: if `Payroll` projects an event that `Timesheets` produces, the graph says Payroll depends on Timesheets. The MCP `dependency-graph` tool shows edges by kind (`usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks`, `shows`, `verifiedWith`, `outsideTheModel`), implied container edges at every level including mixed ones, cycle groups and a suggested story order ([MCP reference](../Documentation/screenplay/mcp/reference.md)). The timeline check reports backward event flow (`PLAY0516`) and mutually dependent sibling groups (`PLAY0517`) as information ([timeline diagnostics](../Documentation/screenplay/imports.md#timeline-diagnostics)).

The graph is evidence, not a complete picture. A name resolves globally and case-insensitively to its earliest owner, a read model resolves to the slice that builds it in preference to the slice that declares its shape, references within one slice are dropped, application triggers are excluded, and code attachments, expressions and module-owned forms contribute nothing ([`DependencyGraph.cs`](../Source/DotNET/Screenplay/Dependencies/DependencyGraph.cs)). Anything checked against the graph therefore checks explicit-reference coupling, not complete architectural isolation.

What the inferred graph cannot say is what the authors *meant*. [#416](https://github.com/Cratis/Screenplay/issues/416) proposes letting a module or feature state its dependencies. A declaration adds two things the graph does not:

- **Stated intent.** "Payroll depends on Timesheets" becomes part of the model a reader sees, not something reconstructed from slices.
- **Drift detection.** When a new slice makes Payroll reach into Engagements, the model says so at the point of change instead of waiting for someone to inspect the graph.

The cost is a new statement in the language, a second list to keep in sync with the slices, extra warnings for teams that opt in, and work in both compilers, the printer, the typed AST, MCP authoring and rename.

Existing rules this record has to fit:

- `uses` is taken on modules and features for behaviors ([grammar](../Documentation/screenplay/grammar.md)), and on operations for systems.
- Notation principles: a construct reads as a statement about the system, prefers qualified operands to new sub-clause keywords and indentation to parentheses ([#81](https://github.com/Cratis/Screenplay/issues/81)); dotted paths are a retained convention ([#350](https://github.com/Cratis/Screenplay/issues/350)).
- Names resolve inside out, innermost first, and a qualified name may use any unambiguous trailing part of the scope path; ties are warnings that name the candidates ([screens](../Documentation/screenplay/screens.md)).
- Module and feature fragments in a folder model merge by name; `authorize` and `uses` accumulate per owner with duplicates kept once and warned ([folders](../Documentation/screenplay/folders.md)).
- Declarations are order-independent ([0023](0023-command-production-model.md)). Authoring metadata such as event `description` or personas adds no ESM bytes ([events](../Documentation/screenplay/events.md#authoring-metadata), [personas](../Documentation/screenplay/personas.md)).
- External event origin is not implemented yet ([0009](0009-external-event-origin-and-translation-slices.md)), so the graph knows other bounded contexts only through contract imports.

## Decision

Adopt option B. A module or a feature may state `depends on <name>` once per target, where the target is a module or a feature. The statement is authoring metadata: it adds no ESM bytes and never changes execution. A container that declares nothing is not checked. Once a container declares at least one dependency, the compiler checks every edge leaving it, including edges from slices in its descendant features, against that container's own declarations. An undeclared inferred dependency is a warning, a declared dependency nothing uses is information, an invalid target is a warning, a repeated identical declaration is a warning and kept once, and two containers that declare each other are reported as information. A container that declares nothing gets no new findings, and no container gets findings because of another container's declarations, except information about mutual declarations on the declaring side. Implementation waits for #414's TypeScript twin of the graph. The details below are part of the decision.

### Spelling

```screenplay
module Payroll
  description "Pays people for approved time"
  depends on Timesheets

  feature Handover
    depends on Timesheets.Approval
    depends on Timesheets.Reporting
    depends on Runs
```

In the TimeTracking sample, `Payroll.Handover` reacts to `TimesheetApproved` from `Timesheets.Approval`, reads `DraftTimesheet` built in `Timesheets.Reporting`, and reads `CurrentPayrollRun` and invokes `AddHoursToPayroll` in its sibling `Payroll.Runs`.

- One target per line. A container with several dependencies repeats the statement. This keeps every target on its own line for diagnostics, rename, merge and diffs, and avoids a list syntax.
- The target is a dotted name, resolved with the existing rules, restricted to modules and features:
  - A bare name resolves inside out: the declaring container's siblings first, then its ancestors' siblings, then modules. `depends on Runs` inside `Payroll.Handover` is the sibling feature `Payroll.Runs`.
  - A dotted name matches any unambiguous trailing part of a container's full address. `Timesheets.Approval` is the `Approval` feature in `Timesheets`.
  - Two equally near matches are an ambiguity warning naming the candidates, as for screens.
  - A sibling feature and a module with the same name resolve to the sibling (innermost wins). A root module has no longer qualifying path, so a root module shadowed by a same-named sibling feature cannot be named at all; the author must rename one of them. This is a real limit on what can be expressed, accepted because the collision is rare.
- `depends`, like other body words, is contextual and only meaningful at the start of a module or feature body line.

### Checking

- **Who is checked.** Each container that declares at least one dependency, on its own. Declaring on `Payroll.Runs` does not opt in `Payroll`, and the reverse.
- **Which edges.** Every edge whose consumer slice is inside the checked container, including slices in its descendant features, and whose producer slice is outside it. Edges inside the container are not its business.
- **Independent enforcement.** Each opted-in container is satisfied only by its own declarations. A descendant's declarations do not satisfy its ancestor's inventory, and an ancestor's declarations do not satisfy a descendant's. In the example, `Payroll` is satisfied by `depends on Timesheets` for all of Handover's edges into Timesheets, while `Payroll.Handover` must name its own targets. `Handover → Runs` is internal to `Payroll` and is checked only for Handover.
- **Satisfaction.** A module target covers edges into any slice in that module; a feature target covers edges into slices in that feature or its sub-features.
- **Edge kinds that count.** `usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks` and `shows`, both for satisfying a declaration and for undeclared findings. A screen showing another module's query or a reaction invoking its command is real coupling. `asks` and `shows` still take no part in story order or cycles, as today. `verifiedWith` is excluded: specifications set up facts from elsewhere as a matter of course. `outsideTheModel` is excluded: it points at a bounded context, which this record does not let a module name (see option D). Unresolved references never become edges, as today.
- **Ambiguous references.** An edge whose producer is ambiguous never raises an undeclared warning. It satisfies a declaration only provisionally: when any alternative lies in a declared target, the declaration is not reported as unused, and the MCP graph view marks the satisfaction as uncertain. No warning is ever derived from an ambiguous ownership.
- **Findings.**
  - Undeclared inferred dependency: **warning**, one per checked container and producer module. It is located at the checked container's header. The message names the producer as a feature path when all the uncovered edges fall in one feature, otherwise as the module, and lists the evidence. Declaring either the module or the specific feature or features satisfies the edges each covers. Duplicate graph nodes collapse as the graph collapses them.
  - Declared dependency nothing uses: **information**, on the declaration. A new module often declares ahead of the slices that use it.
  - Invalid target: **warning** for a target that is the container itself, one of its ancestors or one of its descendants, or that does not resolve. These cannot be checked; the inferred graph excludes the same pairs.
  - Repeated identical declaration on the same container, in one file or across files: **warning**, kept once, like a repeated `authorize`.
  - Mutual declarations: **information**, on the declaring side, when two containers declare each other ("these containers declare each other"). Reciprocal coupling can be legitimate, so it is not a warning.
- **No direction rule.** `depends on X` states that this container may use X. It does not state that X must never use this container, so an edge from X back into the declaring container is not reported against the declaration. One-way, negative and layering rules are deferred (see Options).
- **Cycles.** A declaration never makes a cycle acceptable, and `PLAY0516` and `PLAY0517` are unaffected by declarations.
- **Codes.** Diagnostic codes are allocated by the implementing change, as usual.

### Representation

- **ESM.** Excluded. Executable bytes, revisions and identities do not change, as with personas and event authoring metadata.
- **Syntax and AST.** Module and feature syntax gain an ordered list of declared dependencies, each with its target text and location. Syntax JSON changes are additive. MCP and C# workspaces get typed add and remove edits, and `declaration-details` shows the declarations. `dependency-graph` reports, per edge and checked container, whether it is declared, provisionally declared through an ambiguous reference, or undeclared, and lists declarations nothing uses.
- **Printing.** Printed in authored order, directly after `description`, one line each. Printing and reparsing gives the same syntax.
- **Folder models.** Declarations on the same module or feature accumulate across files, in file-path order, with repeats kept once and warned. Layout expansion writes them only in the owner's own file and strips them from restated headers, like `description`.
- **Rename.** A module or feature rename repairs `depends on` targets as proven typed references. If a target cannot be proven, the rename refuses rather than leaving a stale target, as the planner already does for other references.
- **Both compilers.** The C# and TypeScript compilers report the same findings with the same codes and locations.

## Options considered

- **A. Inferred only, no syntax (status quo after #414).** Not taken as the end state. It costs nothing to keep, but intent lives in prose, and nobody is told when a new slice adds a dependency.
- **B. Explicit `depends on` on modules and features, opt-in, checked against the inferred graph (taken).** Proposed in #416 and recommended by the earlier ordering and dependencies research (option D3). Intent becomes part of the model while the graph stays the evidence, and teams that declare nothing see no change. Prior art: Gradle and Maven "used undeclared / declared unused", Nx module boundaries, ArchUnit and dependency-cruiser rules over an inferred graph.
  - *Module level only* was the research's first recommendation. Not taken: modules alone cannot say that one feature depends on a sibling feature, or on a specific feature in another module, and the graph already resolves those edges.
  - *Slice-level declarations* are not proposed: they would repeat the references the slice already contains.
  - *Declarations on any container between the checked container and the consumer slice satisfy the edge* was the drafted rule. Replaced by independent enforcement: a nested declaration satisfying its ancestor made each container's findings depend on other containers' declarations and blurred what a module had actually stated.
- **C. Explicit only, or required everywhere.** Not taken. Every module would have to list what its slices already show, the list would drift, and requiring it would turn the compiler's own knowledge into busywork. This is the JPMS `requires` end of the range, which fits code packaging, not a business model.
- **D. A context map at `domain` level** (upstream/downstream, anticorruption layer, as in DDD context maps). Deferred, not rejected. It needs [0009](0009-external-event-origin-and-translation-slices.md)'s event origin to know where outside events come from. Until then, relationship kinds can be inferred and shown as labels: an imported event consumed directly reads as conformist, one consumed through a `Translate` slice as translated.
- **Direction, negative and layering rules** (a warning when the target depends back on the declarer, warnings on mutual declarations, `never depends on`, layers). Deferred, not rejected. A positive `depends on X` does not mean "X must never depend on me", and reciprocal declarations can describe legitimate coupling, so reading a direction into it would raise warnings nobody asked for. A separate, explicit rule can be added later without changing anything decided here.
- **Ordering kinds only** (`usesFactsFrom`, `reactsTo`, `decidesFrom`, the kinds the graph uses for cycles and order). Not taken: an `asks` or `shows` edge would then be coupling nobody has to declare.
- **Spelling alternatives.**
  - `depends on feature Approval in Timesheets` is not taken: it adds sub-clause keywords where a qualified operand does the job, which #81 argues against.
  - An indented block listing targets under `depends on` is not taken: one line per target merges, diffs and reports better and needs no new block grammar.
  - `uses` is taken; `requires` reads as a validation rule; `after` confuses dependency with story order.

## Default if unanswered

Superseded by the verdict below. Had the question stayed open, option A would have remained: the inferred graph, the MCP view and the information-level timeline diagnostics, with no syntax. Nothing would have had to be migrated, and adding B later would have been purely additive. The cost was that intent stays in descriptions and conversations and drift is noticed only by someone reading the graph. #387 and #388 work from the inferred graph either way.

## Timeline and scope

The decision holds until a context map at `domain` level is designed, which can extend it. Implementation waits for #414's TypeScript twin, so that both compilers check the same way.

In scope: the `depends on` statement on modules and features, its resolution, the checks and their severities, ESM exclusion, printing, typed AST and MCP support, folder merging and rename.

Out of scope: slice-level declarations, contexts and the context map (option D), direction, negative and layering rules, deriving or applying a story order from declarations, board visualization of declared edges (#415), and any change to `PLAY0516`/`PLAY0517`.

## Verification

**Done when** a module or feature can declare `depends on <module>` or `depends on <module>.<feature>`; both compilers report the undeclared, unused, invalid-target, repeated-declaration and mutual-declaration findings with the same codes, severities and locations on a shared vector set; the declarations add no ESM bytes, round-trip through the printer, merge across files and follow a rename; and a container without declarations reports nothing new.

**Verify by**:

- **Satisfaction in TimeTracking.** Declaring `depends on Timesheets.Approval`, `depends on Timesheets.Reporting` and `depends on Runs` on `Payroll.Handover` raises no undeclared finding for Handover. The edges are: `reactsTo` `TimesheetApproved` (produced in `Timesheets/Approval/ApprovingAWeek.play`) from the `QueueApprovedHours` trigger; `decidesFrom` `DraftTimesheet` from `reads DraftTimesheet` in [`RemindingConsultants.play:10`](../Samples/TimeTracking/Payroll/Handover/RemindingConsultants.play), built by the `TimesheetLifecycle` projection in [`TrackingWeeksOnTheBoard.play:61-62`](../Samples/TimeTracking/Timesheets/Reporting/TrackingWeeksOnTheBoard.play); and `decidesFrom` `CurrentPayrollRun` and `asks` `AddHoursToPayroll` into `Payroll.Runs`. Removing only `depends on Timesheets.Approval` raises one undeclared warning at Handover naming `Timesheets.Approval`, with evidence that resolves to the `QueueApprovedHours` reaction trigger. Removing instead `depends on Timesheets.Reporting` names `Timesheets.Reporting` with evidence at `RemindingConsultants.play:10`.
- **Aggregation.** Removing both Timesheets declarations raises one warning naming the module `Timesheets` and listing both pieces of evidence; adding `depends on Timesheets` alone clears it.
- **Independent enforcement.** With `depends on Timesheets` on `Payroll` and nothing on Handover, Handover gets no findings and Payroll none. With declarations only on Handover, Payroll gets no findings. With `Payroll` declaring `depends on Engagements` only and Handover declaring all three targets, Payroll still reports its undeclared edges into Timesheets: Handover's declarations do not satisfy it.
- **Opt-in boundary and mutual declarations.** Declaring `depends on Payroll` and `depends on Engagements` on `Timesheets` while `Payroll` declares `depends on Timesheets` reports information on each declaring side about the mutual declarations and no warning. `Engagements` is needed because Timesheets reads `EngagementOverview` ([`RecordingHours.play:18`](../Samples/TimeTracking/Timesheets/Recording/RecordingHours.play)); without it Timesheets gets the ordinary undeclared warning naming `Engagements`. `Engagements`, which declares nothing, gets no findings in any of these cases.
- **Unused declarations.** Declaring `depends on Engagements` on `Payroll.Handover` reports information on that line.
- **Invalid targets.** `depends on Handover` inside `Payroll.Handover` (itself), `depends on Payroll` inside it (ancestor), a module declaring one of its own features (descendant), and `depends on Nowhere` (unresolved) each report a warning and contribute no satisfaction.
- **Ambiguity.** A vector with two equally near containers of the same name reports the ambiguity warning naming both. A vector where an event name is declared in two modules gives an ambiguous edge: it raises no undeclared warning whether or not a declaration names either module, prevents an unused finding on a declaration naming either, and is marked uncertain in `dependency-graph`.
- **Shadowing.** In a vector where a module `Runs` exists alongside the sibling feature `Payroll.Runs`, `depends on Runs` in `Payroll.Handover` resolves to the feature.
- **Duplicates.** The same `depends on` repeated in one module header, and again in a second file's fragment of that module, is kept once with one warning per repeat, and printing writes it once.
- **No ESM change.** For each sample that binds to an executable model without errors, the ESM bytes and revision are identical with and without the declarations. Invoicing does not bind today (its dispositions are pinned in `when_binding_the_invoicing_sample`); for it, and for any other sample that does not bind, the syntax with declarations removed is compared for equality, as the layout expansion specs compare samples. The implementing change states which samples fall in each group.
- **Rename.** A module rename through `propose-rename` updates every `depends on` target that names it, and refuses when a target cannot be proven.

## Consequences

- **Easier:** reading a module's place in the system from its header, and catching accidental coupling when it is introduced.
- **Harder:** teams that opt in maintain a list next to the slices, at every level they opt in, and `--warnaserror` builds fail on undeclared dependencies. That is the point of opting in, but it is real friction. Every tool that edits modules and features (printer, AST, MCP, rename, layout expansion) gains one more element to carry. A root module hidden by a same-named sibling feature cannot be declared without a rename.
- **Limits:** declarations check the explicit references the compiler can resolve, so code attachments, expressions and module-owned forms can couple containers without a finding.
- **Forecloses:** using `depends on` with a different meaning, such as ordering, a direction rule or runtime deployment. It also commits the module and feature header to authoring metadata that tooling must keep in sync, which is why the decision is costly to reverse once models use it.

## Verdict

The decider delegated this verdict to the orchestrating agent on 2026-10-07; the choices below were made under that delegation after an independent cross-provider critique.

1. **Explicit declarations now (B), or stay with the inferred graph (A)?** B, implemented after #414's TypeScript twin: intent and drift detection are worth an opt-in statement that costs nothing to teams that do not use it.
2. **Module and feature level, or modules only?** Both: modules alone cannot express sibling-feature or cross-module-feature dependencies.
3. **Should `asks` and `shows` count?** Yes, for satisfaction and for undeclared findings, but not for story order or cycles: they are real coupling, not ordering.
4. **Are the severities right?** Undeclared is a warning, unused is information, an invalid target is a warning, and a repeated declaration is a warning kept once. The direction and mutual-declaration warnings are dropped: a positive `depends on X` does not forbid X from depending back, and mutual declarations are reported as information.
5. **Is the spelling acceptable?** Yes: `depends on X` or `depends on X.Y`, one target per line, resolved inside out with the sibling winning over a same-named module; the unnamable shadowed root module is a documented, accepted limit because it is rare.
6. **Can a container that never opted in get findings?** No. A container that declares nothing gets no new findings, and no container gets findings because of another's declarations, except information about mutual declarations on the declaring side.
7. **How are ambiguous references treated?** They never raise an undeclared warning and only provisionally satisfy a declaration, shown as uncertain in the graph view: no warning is derived from uncertain ownership.
8. **Do nested declarations satisfy an ancestor?** No. Each opted-in container is checked against its own declarations for every edge leaving it, including edges from its descendant features: what a container states stays what it is held to.
9. **How are undeclared findings grouped?** One warning per checked container and producer module, at the container's header, naming the feature when all uncovered edges fall in one, otherwise the module, with evidence: one actionable finding per missing declaration rather than one per reference.

## Status notes

**2026-10-07 — verification wording.** The opt-in example above says the warning names `Engagements`. Under the naming rule in Checking it names `Engagements.Portfolio`, because every uncovered edge falls in that one feature. The rule is unchanged; only the example's wording was imprecise ([#416](https://github.com/Cratis/Screenplay/issues/416)).

**2026-10-07 — naming within the container's own module.** When a feature's uncovered edges go to several features of its own module, naming the module would point at the feature's own ancestor, which is an invalid target. In that case the single warning for that module lists the features that can be declared instead. The grouping (one warning per checked container and producer module) is unchanged ([#416](https://github.com/Cratis/Screenplay/issues/416)).
