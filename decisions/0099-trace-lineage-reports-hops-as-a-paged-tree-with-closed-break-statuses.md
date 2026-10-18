---
id: 0099
title: The lineage report pages hops as a tree with closed roles and break statuses
status: proposed
stage: none
class: contract
reversibility: reversible
applies-to:
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/mcp/**
---

## Context

"Where does this field come from?" is a common question when reviewing an event model. Today the answer takes
one `dependencies` call per hop, and the reviewer stitches the hops together by hand (Cratis/Screenplay#388). The
property-link index behind `dependencies` and `find-references` already holds every hop the question needs:
projection mappings and AutoMap, production, capture and invocation mappings, and screen and form bindings. It
exposes them one direct edge at a time and never as a path.

The owner has already settled the surrounding scope on #388: reports are advisory and MCP-only, with no CLI command.
Rule ids use the `<FAMILY><nnn>` form. Unread event properties ship as `SMELL006`. Structural completeness checks
beyond PLAY0530-0537 are deferred with acknowledgements in #448. What was still open is the lineage report's
contract: what a hop is, and how a break or an ambiguity reads. Several MCP clients and the agent skills will build
on that answer, so it should not drift between releases.

## Decision

The MCP tool `trace-lineage` takes one read-model property `address` (the owner's address followed by the member
name, as for `kind: "Property"` elsewhere) and returns one revision-bound page of **hops**. A hop is either a
declaration member (a property) or the authored clause that connects two members (a mapping, a binding or a code
body). Every hop carries an integer `id` and the `parent` id it was reached from, plus `depth`, `direction`
(`subject`, `downstream` or `upstream`), `role`, `status`, the `kind` and `address` that `declaration-details`
resolves (null only for literal, context and operation values), and the authored `location`. The order is
deterministic: the subject first, then its downstream readers, then the upstream sources depth-first, with siblings
sorted by address and location. Several children under one hop are **branches**: several real sources that all
apply, such as two events feeding one property or two commands producing one event.

Roles form a closed set. Downstream: `query` (returns the owner read model) and `screen-display` (a screen or form
binding shows the property). Members: `read-model-property`, `event-property` and `command-property`. Connecting
clauses: `projection-mapping` (with `mode` explicit or automap and `block` from, join or all),
`production-mapping` (command or reaction `produces`), `capture-mapping` (capture `append`), `invocation-mapping`
(reaction `invokes`), `interaction-mapping` (a screen action argument) and `code`. Terminals: `screen-input` (a form
field for the command), `literal`, `context` (for example `$eventSourceId` or `$context.occurred`), `operation`
(count, increment, decrement, clear), `capture-source` (an outside source-item path) and `external` (an event
declared with an origin and produced by nothing local). A command property is the model's input boundary.
Screen inputs hang under it when the model declares them, and their absence is not a break.

Statuses form a closed set too. `resolved` and `repeated` (a member already reached, with `repeatOf`, so the trace
is not expanded twice) are not breaks. A break is a leaf. The trace never continues past one, and each break states
its `reason`:

- `missing`: a hop that should exist does not. It is a placeholder whose `role` names the absent clause, under
  the member that lacks it, for example a `projection-mapping` under an unmapped read-model property. When a
  producer exists but does not map the member, the placeholder carries that producer's address.
- `ambiguous`: one authored name resolves to several declarations. The hop lists them as `candidates` and none is
  followed.
- `incomplete`: the index has the link but cannot resolve it fully, for example a nested path, a child or nested
  projection scope, or a raw expression.
- `opaque`: the next step is code, such as a reducer, a command handler, a reaction trigger body or a performer
  query (decision 0097). The hop names the declaration that owns the code, and the trace does not guess what the
  code reads.
- `truncated`: the budget of 500 hops and depth 24 is spent.

Every page repeats one summary computed over the whole trace: `hopCount`, `complete` (true only when the trace has
no break), `truncated`, and a bounded `breaks` list (id, status, role, address and reason, with at most 50 entries
and a `breaksTruncated` flag). Hops are paged with `offset` and `limit` (default 50, maximum 200) inside the
standard response byte budget. A continuation page requires `expectedSourceRevision` and refuses a stale source,
as every other MCP report does.

## Options considered

- **A nested JSON tree.** Rejected: it cannot be paged, and its depth grows with the model. A flat list with parent
  ids keeps the same tree, and each page stays bounded.
- **One linear chain.** Rejected: fan-in is normal. A single chain would have to pick one feeding event, and that
  pick would be a guess.
- **The mapping as an attribute of the edge rather than a hop.** Rejected: the reviewer usually needs to go to the
  mapping itself. It has its own owner (the projection, command or screen) and its own location.
- **Following every candidate of an ambiguous name.** Rejected: that presents a guess as lineage. The candidates
  are listed, and the reviewer picks one.
- **Reporting breaks as rule-id findings in a completeness report.** Rejected for now: completeness rules are
  deferred with #448. A lineage break is a fact about one trace, not a model-wide rule, and a later completeness
  report can build on this tool's statuses.
- **Accepting a screen field as the subject.** Deferred: a screen field reaches its read-model property through a
  `screen-binding` link that `find-references` already shows. Accepting it here is additive.

## Default if unanswered

There is no lineage tool. Reviewers and agents keep chaining `dependencies` calls, one per hop and dozens per field,
and each client invents its own wording for "broken", "ambiguous" and "code". A field fed by an unmapped event
stays easy to miss because nothing reports the gap.

## Timeline and scope

The contract holds for the current language freeze (0092) and until completeness rules land with #448. Adding a
role, a status or an optional input is additive. Renaming or removing one supersedes this record.

In scope: the MCP tool, its hop and summary shape, paging, revision binding and budgets, its specifications and the
MCP reference documentation.

Out of scope: transitive runtime impact, scenario or specification coverage (#390, #395), structural completeness
rules (#448), a CLI command, event-source identity lineage (`$eventSourceId` and `for` destinations stay `context`
terminals), historical event generations (the trace follows the current generation), and any language, diagnostic
or ESM change.

## Verification

Done when `trace-lineage` is registered in the MCP tool catalog with the contract above, the contract golden lists
it, and specifications cover a full Library chain from `CatalogEntry.title` to the `AddBook` form input with every
address resolving through `declaration-details`, an unmapped read-model property reported as `missing` at the
`projection-mapping` hop, explicit, AutoMap and join mapping hops, a reducer reported as `opaque`, and two producing
events reported as branches.

Verify by running `dotnet test Source/DotNET/Screenplay.Mcp/Screenplay.Mcp.csproj --filter
FullyQualifiedName~for_McpLineage` and the Screenplay.Contracts golden test, both passing, and by reading the
`trace-lineage` section of `Documentation/screenplay/mcp/reference.md`.

## Consequences

A reviewer or an agent gets a field's lineage, with the break located, in one paged call. Fan-in, ambiguity and
code each have their own name, so a client no longer has to tell them apart by heuristics.

The vocabulary is now a contract: the role and status names cannot be renamed casually. Lineage stays exactly as
complete as the property-link index. A hop the index does not classify, such as a legacy command read or a nested
path, reads as `incomplete` rather than being inferred.

## Related issues

Cratis/Screenplay#388, #390, #395, #448; decisions 0092, 0097.
