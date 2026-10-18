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
| [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) | Allocate ESM v7 to generated values and responses, and number later versions at admission | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0026](0026-generated-values-and-command-responses-in-esm-v7.md) | Admit generated values and command responses as ESM v7 | accepted | none | 2026-10-06 | Sindre Alstad Wilting |
| [0027](0027-policy-negation-joins-esm-v7.md) | Admit policy negation as a byte-preserving ESM v7 extension | accepted | implemented | 2026-10-07 | Sindre Alstad Wilting |
| [0028](0028-declared-module-and-feature-dependencies.md) | Let modules and features declare what they depend on, checked against the inferred graph | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0029](0029-guarded-screen-action-renderer-contract.md) | Select guarded screen commands in authored order without authorization fall-through | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0030](0030-reaction-refusals-and-redelivery.md) | Reaction refusal handling and redelivery specifications | accepted | implemented | 2026-10-07 | Sindre Alstad Wilting |
| [0031](0031-event-source-and-stream-in-specifications.md) | State the event source and stream of specification events with the command route's own lines | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0032](0032-expand-typed-specification-examples-in-the-front-end.md) | Expand typed specification examples in the front end | accepted | none | 2026-10-07 | Sindre Alstad Wilting |
| [0033](0033-composite-event-stream-ids.md) | Declare composite event stream ids as named parts and encode them with an escaped separator | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0034](0034-sensitive-means-operational-secret.md) | Treat sensitive values as operational secrets, not personal data | accepted | implemented | 2026-10-08 | Sindre Alstad Wilting |
| [0035](0035-keep-model-reasoning-as-report-only-metadata.md) | Keep model reasoning as report-only authoring metadata | accepted | implemented | 2026-10-08 | Sindre Alstad Wilting |
| [0036](0036-admit-event-sources-streams-and-command-routes.md) | Admit event sources, streams and command routes into the executable model, with specification routes and composite stream ids | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0037](0037-routes-in-redelivery-locators-and-specification-examples.md) | State routes in redelivery locators and in typed specification examples with the existing route lines | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0038](0038-move-logical-subtrees-with-identity-continuity.md) | Move logical slices and features across parents with identity and behavior continuity | accepted | none | 2026-10-08 | Sindre Alstad Wilting (delegated to the implementing agent's recommendation) |
| [0039](0039-publish-a-machine-readable-screenplay-contract.md) | Publish a machine-readable Screenplay contract | accepted | implemented | 2026-10-08 | Sindre Alstad Wilting (delegated to the implementing agent's recommendation) |
| [0040](0040-description-on-concepts-rules-and-ui.md) | Accept body-line descriptions on concepts, policies, constraints, projections, screens and forms | accepted | implemented | 2026-10-08 | Sindre Alstad Wilting |
| [0041](0041-personal-data-secrets-and-processing-purposes.md) | Mark personal data and secrets on concepts, and declare processing purposes once | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0042](0042-persona-callers-in-specifications.md) | Expand persona callers in specifications and check persona coverage on request | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0043](0043-reaction-identity-runs-as.md) | Declare the system identity under which a reaction's invoked commands run | accepted | implemented | 2026-10-08 | Sindre Alstad Wilting |
| [0044](0044-evolve-event-properties-through-mcp.md) | Add, rename and remove a property along its mapping chain through a planned proposal, with an explicit event-evolution choice | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0045](0045-model-notes-sidecar.md) | Record questions, tasks and notes against model elements in a committed sidecar outside the semantic model | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0046](0046-specification-case-tables.md) | Run one specification over named cases, expanded in the front end | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0047](0047-data-subject-mark-on-event-properties.md) | Mark an event's data subject with a trailing subject modifier on a property (phase 1) | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0048](0048-guarded-on-bindings.md) | Select whole interaction action lists with ordered item conditions | accepted | none | 2026-10-08 | Sindre Alstad Wilting |
| [0049](0049-public-events-and-translation-direction.md) | Mark public events, state their origin as an opaque quoted store name, and give Translate slices a direction | accepted | none |  |  |
| [0050](0050-public-event-execution-semantics.md) | Define the executable meaning of public events, outbound translation, event targets and reaction results | accepted | none | 2026-10-09 | Einar Ingebrigtsen |
| [0051](0051-admit-public-events-as-esm-v9.md) | Admit public events, translation direction, event-target projections and event-source captures as ESM v9 | accepted | none | 2026-10-09 | Einar Ingebrigtsen |
| [0052](0052-identity-details-as-authoring-metadata.md) | Declare one caller detail shape as authoring metadata before executable admission | accepted | implemented | 2026-10-09 | Sindre Alstad Wilting |
| [0053](0053-public-structural-model-comparison.md) | Share typed structural model comparison from the Screenplay library | accepted | implemented | 2026-10-10 | Sindre Alstad Wilting |
| [0054](0054-concept-generation-strategies-and-freshness.md) | Declare how a concept generates its values, and refuse a generated value that is not fresh (V21 phase-3 admission; generated secrets pending) | accepted | none | 2026-10-11 | Sindre Alstad Wilting |
| [0060](0060-declare-read-model-keys-and-bind-lookups-by-name.md) | Declare read-model keys on properties and supply every key part by name in lookups | accepted | implemented | 2026-10-10 | Sindre Alstad Wilting |
| [0061](0061-per-production-route-overrides.md) | Replace the command route per event production | accepted | implemented | 2026-10-10 | Sindre Alstad Wilting |
| [0062](0062-observer-source-and-stream-filters.md) | Filter reactions and reducers by event source and stream | accepted | implemented | 2026-10-10 | Sindre Alstad Wilting |
| [0063](0063-admit-production-routes-and-observer-filters-as-esm-v10.md) | Join the claimed unreleased ESM v10 with production routes and observer filters | accepted | implemented | 2026-10-10 | Sindre Alstad Wilting |
| [0064](0064-exact-number-literals-claim-esm-v11.md) | Claim ESM v11 for exact Double-mode number literal lowering | accepted | implemented | 2026-10-11 | Sindre Alstad Wilting |
| [0065](0065-explicit-enum-member-numbers.md) | Give enum members optional explicit numbers, all or none, and carry them in a later ESM version | accepted | implemented | 2026-10-11 | Sindre Alstad Wilting |
| [0066](0066-published-esm-versions-are-frozen.md) | Freeze an ESM version when any package exposing it is published | accepted | implemented | 2026-10-11 | Sindre Alstad Wilting |
| [0067](0067-report-every-selected-event-property-evolution.md) | Report every explicitly selected event property evolution in one generations array | accepted | none | 2026-10-11 | Sindre Alstad Wilting |
| [0068](0068-admit-concept-compliance-markers-as-esm-v13.md) | Admit concept compliance markers into the executable model as ESM v13 | accepted | implemented | 2026-10-12 | Sindre Alstad Wilting |
| [0069](0069-admit-equality-query-filters-as-esm-v14.md) | Admit caller-supplied equality query filters into the executable model as ESM v14 | accepted | implemented | 2026-10-12 | Sindre Alstad Wilting |
| [0070](0070-derive-event-mutability-from-an-admitted-release-baseline.md) | Derive event mutability from an admitted release baseline committed beside the identity catalog | accepted | none | 2026-10-13 | Sindre Alstad Wilting |
| [0071](0071-whitespace-only-text-does-not-satisfy-not-empty.md) | Whitespace-only text does not satisfy `not empty` | accepted | implemented | 2026-10-13 | Sindre Alstad Wilting |
| [0072](0072-query-arguments-are-validated-against-concept-rules.md) | Query arguments are validated against their concept rules, and a violation is a rejection | accepted | none | 2026-10-13 | Sindre Alstad Wilting |
| [0073](0073-reserve-the-stored-event-stream-name-all.md) | Reserve the stored event stream name All | accepted | none | 2026-10-13 | Sindre Alstad Wilting |
| [0074](0074-admit-no-event-assertions-and-redelivery-before-refusal-branches.md) | Admit explicit no-event assertions and redelivery before reaction refusal branches | accepted | none | 2026-10-13 | Sindre Alstad Wilting |
| [0075](0075-carry-the-stored-event-type-name-in-the-executable-model.md) | Carry the stored event type name in the executable model | accepted | none | 2026-10-13 | Sindre Alstad Wilting |
| [0076](0076-declare-the-validation-gate-in-a-model-root-file.md) | Declare the validation gate in an authored screenplay.json at the model root | accepted | implemented | 2026-10-14 | Sindre Alstad Wilting |
| [0077](0077-present-consumed-public-events-to-captures-over-events.md) | Present a consumed public event to a capture over events as the event itself, and fold it into per-key state as Chronicle does | accepted | implemented | 2026-10-14 | Sindre Alstad Wilting |
| [0078](0078-opt-into-atomic-reaction-production-batches.md) | Opt into atomic reaction-production batches without changing existing models | accepted | implemented | 2026-10-15 | Sindre Alstad Wilting |
| [0079](0079-warn-when-a-directionless-translate-slice-reacts-to-its-own-capture.md) | Warn when a directionless Translate slice reacts to an event its own capture appends | accepted | implemented | 2026-10-15 | Sindre Alstad Wilting |
| [0080](0080-treat-the-first-record-of-a-key-in-a-record-capture-as-added-only.md) | Treat the first record of a key in a record capture as added only, as Chronicle does | accepted | implemented | 2026-10-15 | Sindre Alstad Wilting |
| [0081](0081-choose-the-active-sample-data-pack-once-in-the-ui-profile.md) | Choose the active sample data pack once in the ui profile | accepted | implemented | 2026-10-15 | Einar Ingebrigtsen |
| [0082](0082-admit-a-described-command-handler-as-a-pending-implementation-requirement.md) | Admit a described command handler as a pending implementation requirement with a fixed result contract | accepted | none | 2026-10-15 | Sindre Alstad Wilting |
| [0083](0083-bind-the-generation-context-per-language-target.md) | Bind the version-1 generation context to each language target's native type | accepted | none | 2026-10-15 | Sindre Alstad Wilting |
| [0084](0084-define-matches-patterns-as-a-portable-linear-dialect.md) | Define matches patterns as a portable linear dialect and evaluate them without a timeout | accepted | none | 2026-10-15 | Sindre Alstad Wilting |
| [0085](0085-admit-protected-command-reads-through-an-explicit-preamble.md) | Admit protected command reads in the next ESM version through an explicit preamble | accepted | none | 2026-10-15 | Sindre Alstad Wilting |
| [0086](0086-give-diagnostics-named-typed-arguments-declared-per-code.md) | Give diagnostics named, typed arguments declared per code | accepted | none | 2026-10-16 | Sindre Wilting (delegated to the L0 coordinator) |
| [0087](0087-admit-chronological-date-and-datetime-ordering-in-command-conditions.md) | Admit chronological Date and DateTime ordering in command conditions and record the receipt clock rule | accepted | none | 2026-10-16 | Sindre Wilting (delegated to the L0 coordinator) |
| [0088](0088-admit-composite-read-model-keys-into-the-executable-model.md) | Admit composite read-model keys into the executable model as flat key parts | accepted | none | 2026-10-16 | Sindre Wilting (delegated to the L0 coordinator) |
| [0089](0089-expose-the-reference-evaluators-decide-commit-split-to-hosts.md) | Expose the reference evaluator's decide/commit split to hosts and publish protected-read race vectors | accepted | none | 2026-10-16 | Sindre Wilting (delegated to the L0 coordinator) |
| [0090](0090-admit-uuid-equality-in-command-conditions.md) | Admit Uuid equality in command conditions | accepted | none | 2026-10-16 | Sindre Wilting (delegated to the L0 coordinator) |
| [0091](0091-protected-reads-treat-keys-the-event-store-refuses-as-missing.md) | Protected reads treat keys the event store refuses as missing | accepted | none | 2026-10-17 | Sindre Wilting (delegated to the L0 coordinator) |
| [0092](0092-the-language-is-frozen-at-esm-v28.md) | The language is frozen at ESM v28 | accepted | none | 2026-10-17 | Sindre Wilting (delegated to the L0 coordinator) |
| [0093](0093-give-containers-identities-before-the-model-binds.md) | Give applications, modules, features and slices identities before the model binds | accepted | none | 2026-10-17 | Sindre Wilting (delegated to the L0 coordinator) |
| [0094](0094-report-enum-numeric-identity-changes-in-model-comparison.md) | Report enum numeric identity changes in model comparison | accepted | none | 2026-10-18 | Sindre Wilting (delegated to the L0 coordinator) |
| [0097](0097-a-performer-query-is-an-opaque-read-not-a-refusal.md) | A performer query is an opaque read, not a refusal of the application | proposed | none | — | — |
