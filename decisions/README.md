# Decision records

| ID | Title | Status | Stage | Decided | Decider |
| --- | --- | --- | --- | --- | --- |
| [0001](0001-chronicle-runtime-semantic-authority.md) | Use Chronicle's runtime meaning for portable executable semantics | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0002](0002-implementation-attachments-envelope-and-reducer-role.md) | Implementation attachments: envelope first, reducer transitions as the first role | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0003](0003-decision-consistency-for-command-reads.md) | Decision consistency for command reads | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0004](0004-admission-and-governance-of-portable-executable-semantics.md) | Admission and governance of portable executable semantics | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0005](0005-policy-predicates-as-an-implementation-attachment-role.md) | Policy predicates as an implementation attachment role, composed in authored order | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0006](0006-reaction-triggers-declare-reads.md) | Reaction triggers declare the views they decide from with reads | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0007](0007-affected-read-model-instances.md) | Which read-model instances an event affects follows Chronicle's keys and joins | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0008](0008-one-data-subject-per-event.md) | Personal data in an event belongs to one subject | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0009](0009-external-event-origin-and-translation-slices.md) | External events declare their origin; translating them is a Translate slice | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0010](0010-query-paging-ordering-and-live-delivery.md) | Queries: page-number paging, one sort field, change-set live delivery, unordered by default | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0011](0011-event-generations.md) | Event generations are declared in full, each succeeding the one before | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0012](0012-typed-context-descriptor-and-command-handler-role.md) | A language-neutral typed-context descriptor, then command handlers as the next role | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0013](0013-equivalence-for-screenplay-code-round-trips.md) | What "equivalent" means for Screenplay and code round trips | accepted | none | 2026-09-24 | Sindre Alstad Wilting |
| [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md) | Diagnostic repairs are typed workspace proposals | accepted | implemented | 2026-09-24 | Sindre Alstad Wilting |
| [0015](0015-event-generations-in-the-executable-model.md) | Event generations in the executable model | accepted | implemented | 2026-09-25 | Sindre Alstad Wilting |
| [0016](0016-exporting-the-executable-model-over-mcp.md) | Exporting the executable model over MCP | accepted | implemented | 2026-09-25 | Sindre Alstad Wilting |
| [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) | Map declared decision reads to Chronicle decision reads | accepted | none | 2026-09-28 | Sindre Alstad Wilting |
| [0018](0018-provider-identifiers-and-portable-default-tenant.md) | Use provider-generated identifiers in code bodies and keep the portable default tenant | accepted | implemented | 2026-09-26 | Sindre Alstad Wilting |
| [0019](0019-publish-the-context-family-separately.md) | Publish the context family in a slim package with compiler type forwarding | accepted | implemented | 2026-09-26 | Sindre Alstad Wilting |
| [0020](0020-keyed-read-model-absence-in-esm-v5.md) | Admit keyed read-model absence assertions in ESM v5 | accepted | none | 2026-09-26 | Sindre Alstad Wilting |
| [0021](0021-commands-produce-events-operations-and-responses.md) | Commands produce events, operations and responses | superseded | none | 2026-09-28 | Sindre Alstad Wilting |
| [0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md) | Admit clocks, application triggers, capture records and reactions into specifications as ESM v6 | accepted | implemented | 2026-10-02 | Einar Ingebrigtsen |
| [0023](0023-command-production-model.md) | Model command productions and allocate their ESM versions | accepted | none | 2026-10-02 | Sindre Alstad Wilting |
| [0024](0024-exact-numeric-source-mode.md) | Exact numeric source mode and its ESM admission | proposed | none | — | — |
| [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) | Allocate ESM v7 to generated values and responses, and number later versions at admission | accepted | none | 2026-10-06 | Sindre Alstad Wilting |
| [0026](0026-generated-values-and-command-responses-in-esm-v7.md) | Admit generated values and command responses as ESM v7 | accepted | none | 2026-10-06 | Sindre Alstad Wilting |
| [0027](0027-policy-negation-joins-esm-v7.md) | Admit policy negation as a byte-preserving ESM v7 extension | accepted | implemented | 2026-10-06 | Sindre Alstad Wilting |
| [0028](0028-declared-module-and-feature-dependencies.md) | Let modules and features declare what they depend on, checked against the inferred graph | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0029](0029-guarded-screen-action-renderer-contract.md) | Select guarded screen commands in authored order without authorization fall-through | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0030](0030-reaction-refusals-and-redelivery.md) | Reaction refusal handling and redelivery specifications | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0031](0031-event-source-and-stream-in-specifications.md) | State the event source and stream of specification events with the command route's own lines | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0032](0032-expand-typed-specification-examples-in-the-front-end.md) | Expand typed specification examples in the front end | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
