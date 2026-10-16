---
id: 0086
title: Give diagnostics named, typed arguments declared per code
status: accepted
stage: none
decided: 2026-10-16
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay.ContractCatalog/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/diagnostics.md
---

## Context

A diagnostic is a code, a severity, a message and a location in both compilers. Its subjects (which specification, which
step, which property) appear only inside the message text. Hosts that want to present them, such as Studio Preview
explaining why a model does not compile (Cratis/StudioIssues#678), either show the text as it is or parse it, and parsing
breaks whenever wording changes. Studio today re-parses the document and matches source locations to find the step
behind PLAY0524 (#804).

## Decision

1. **Shape.** A diagnostic gains `arguments`: a map from a stable argument name to a value `{ kind, value }`. The kinds
   are:
   - `text`: authored text, verbatim;
   - `keyword`: a member of a closed language set such as a step role or a declaration kind. The value is the
     language's own keyword spelling, which is stable and never localized, and the catalog declares the closed set for
     each such argument, so a host can switch on it;
   - `index`: a zero-based number;
   - `declaration`: `{ kind, path }`. `kind` is the declaration keyword, for example `specification`, `command`, `event`,
     `readmodel` or `slice`. `path` is a list of authored names: `[module?, ...features, slice?, name]`.
     - Features appear outermost first.
     - A scope that does not exist, such as a declaration at document level, is left out.
     - For a slice itself the path ends with the slice name, which is never repeated.
     - Example: `["Billing", "Invoices", "Overdue", "SendReminder", "reminds the customer"]` is a specification in
       slice `SendReminder`, feature `Overdue`, feature `Invoices`, module `Billing`.
     - Example: `["Customer"]` is a document-level read model.
     - A specification's path uses its authored name, never an expanded case name; the `case` argument carries the case.
   Values are target-neutral data, never presentation and never a language-specific type name. A diagnostic without
   arguments has an empty map. The field is additive: existing constructors, helpers and JSON readers keep working.
2. **Declared per code.** Each code declares its arguments next to the code constant. For each argument it declares the
   name, the kind, whether the argument is optional, a one-line meaning, and for `keyword` arguments the closed set of
   values. The declaration flows into the generated diagnostic catalog and the published contract golden.
   - **Permanent once published.** A published argument's name, kind, meaning and optionality never change, and it is
     never removed. A change that would break this needs a new argument name, or a new code.
   - **Compatible changes.** Adding an optional argument to a code, or adding a value to a keyword set. Hosts therefore
     must tolerate keyword values they do not know.
   - Tools may rely only on declared arguments. The message text stays for people.
3. **Both compilers emit the same arguments** for every code they share. Shared conformance vectors hold them equal.
   Arguments compare by value, so de-duplicating diagnostics keeps working.
4. **Hosts.** The MCP diagnostics views and the CLI's JSON output carry `arguments`. Text output is unchanged.
5. **Order of work.** Phase 1 covers the specification and mapping codes Studio surfaces, starting with PLAY0524:
   `specification` (declaration), `step` (keyword: the step's role), `stepIndex` (see below), `property` (text), `type` (text), and `example` and `case`
   where they apply. Phase 1 also covers PLAY0518–0523, 0525, 0526, 0351, 0468, 0583, 0101, 0044, 0075 and 0514. Other
   codes follow family by family. PLAY0198 gets arguments only after it is split into specific codes.
   `stepIndex` is the zero-based position of the step among the specification's authored steps, in source order. Each
   step clause that starts with a step keyword (`given`, `when` or `then` in any of their forms) counts once. Lines
   nested under a step, such as property assignments, rows and expected results, belong to that step. Steps added from
   outside the specification's own source (synthesized callers, defaults) do not count. Expanding examples keeps the
   authored position, and `case` identifies the expanded case.
   `diagnostics.md` lists every code's arguments and has a short section for tool authors: rely on declared arguments
   only, never on message text.

## Options considered

- **A typed record per code.** Strong typing, but it means hundreds of types and their TypeScript mirrors, a
  polymorphic wire shape, and churn whenever a reporting site changes.
- **Semantic addresses only.** The TypeScript compiler cannot produce them, parse-time diagnostics have no
  application identity, and properties and values have no address. A C#-only optional address on `declaration` values
  can come later.

## Default if unanswered

Hosts keep parsing message text, and every rewording breaks them.

## Verification

- Codes that both compilers emit: shared conformance vectors assert identical arguments.
- Codes only the C# binder emits, such as PLAY0524: binder specs assert the arguments.
- A catalog spec over the canonical corpus and the samples checks three things: every emitted argument is declared for
  its code, every required argument is present, and every keyword value is in its declared set.
- Diagnostics with the same arguments in any order are equal and hash equal. JSON round-trips old diagnostics (no
  `arguments`) and new ones. An empty argument map serializes as `{}`.
- The contract golden carries each code's argument table and is regenerated through its switch and reviewed. The
  documentation is held to the catalog.
- The MCP and tool JSON specs show `arguments`.

## Related

#804. Consumers: Studio Preview (Cratis/StudioIssues#678 follow-up), the Screenplay MCP diagnostics views, and the JSON
output of the Screenplay command-line tool in this repository. The `cratis` CLI exposes them only through what it
serializes from this library, which belongs to the cli repository.
