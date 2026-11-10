---
id: 0153
title: Systems declare abilities, extensions realize them, and every behavior calls them durably
status: proposed
stage: none
class: contract
reversibility: costly
amends: 0124
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Tool/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Screenplay cannot have a construct for everything, just as it cannot draw everything. A model still has to say
that an order posts to the warehouse channel in Slack, books a pickup through a carrier's REST API, or asks a
manager in Teams to approve a backorder. Today it can only half say it:

- `system` and `operation` ([operations.md](../Documentation/screenplay/operations.md), #301) name an external
  system and a command's pre-commit work as **intent**. A `system` has no abilities (#301, open decision 2), so an
  operation describes its call only loosely, and binding refuses both with `PLAY0268`.
- Reactions run attached bodies through the body-execution seam (decision 0121), but only to return events or
  commands. They cannot reach the outside world in a way the model can see, retry or specify.
- Workflows and state machines (decision 0124) send commands, wait and ask for approval. They cannot talk to
  anything that is not a command, and they cannot carry inline code.
- Application `trigger`s and the trigger registry name inbound signals, but nothing connects them to the outbound
  call that caused them, such as a reply to a Slack question.

Stage already runs workflows and state machines on an Orleans silo through Cratis Choreography. Choreography
resolves a node's work through an `IWorkflowNodePerformer` keyed by a `CapabilityReference` (`id@version`),
pins a provider version per run (`CapabilityPin`), retries with an idempotency key, and stops a run for
reconciliation when an outcome is ambiguous. That seam is the runtime this design needs; it is not yet open to
anything but sending commands and evaluating decisions (`StageWorkflowPerformers`).

The request is one extension story for every behavior (commands, reactions, workflows and state machines), with
inline code as an equal citizen and a durable, message-oriented, Orleans-based runtime in Stage, in the
language's existing style.

## Decision

The design is three constructs that already have a place in the language, plus one verb:

| Concern | Construct | Analogy in the language today |
| --- | --- | --- |
| *What* the business can ask of the outside world | `ability` blocks on a `system` | A `command`'s typed inputs and `returns` |
| *How* it is done (REST, Slack, Teams, code, ...) | an `extension` line on the `system`, or an `implementation` on the ability | A `sink` (logical in the model, bound in deployment) and an attachment (decision 0002) |
| *Doing it*, from any behavior | `calls <System>.<Ability>` | `invokes <Command>` and `sends <Command>` |
| Pure inline code inside a process | a `compute` node in a workflow | A reducer or rule body |

Three principles hold the design together:

1. **The model names the business system and what it can be asked; the extension owns the technology.** The
   model never says SMTP, Slack API or `HttpClient`. Swapping Slack for Teams changes one `extension` line, not a
   single call site.
2. **Code computes; abilities touch the world.** Inline code that only computes runs anywhere, any number of
   times. Anything with an external effect goes through an ability, so it gets an identity, an idempotency key,
   retries, a recorded outcome and a place in specifications.
3. **Outcomes become facts only when the model says so.** A call's result is never turned into an event
   implicitly. A branch says which event records which outcome, just as reaction refusal branches do.

### 1. A `system` declares abilities

`system` stays the outbound counterpart of `trigger` (#301). It gains `ability` blocks: a typed contract with
inputs, an optional `returns` block, named business `outcome`s and an effect class.

```screenplay
concept ParcelId : Uuid
concept TrackingNumber : String

system Carrier
  description "The parcel carrier's booking service"
  ability BookPickup idempotent
    description "Books a pickup for one parcel"
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
      description "The carrier has no pickup slot on the requested day"
  ability CancelPickup idempotent
    trackingNumber TrackingNumber
```

- **Inputs** use the property grammar of commands and operations: concepts, enums, composites, collections and
  `optional`. Like operation inputs, they cannot be `identifier` or `generated`.
- **`returns`** uses the response field grammar of a command's `returns` block. An ability without `returns`
  returns nothing.
- **`outcome <Name>`** names a business result other than success, such as a refusal the external system gives.
  Outcomes are the external counterpart of `on refused by constraint <Name>`; an outcome carries no payload in
  this version (open question 3).
- **The effect class** says what a retry may do, and drives the runtime in section 6:

  | Class | Meaning | When an attempt's outcome is unknown |
  | --- | --- | --- |
  | `safe` | No external effect (a lookup). | Retry freely. |
  | `idempotent` | Repeating with the same idempotency key has the effect once. | Retry with the same key. |
  | `once` (default) | Repeating may repeat the effect, as when posting a message without deduplication. | Never retry: stop for reconciliation. |

  `once` is the default because it is the only class that is never wrong; an author opts into retries by saying
  the system can take them.
- **Ability names** are unique within their system, compared ignoring case. A system with abilities may still be
  used by intent-only `operation ... uses <System>` declarations.

### 2. An extension or an implementation realizes a system

A system says how its abilities are realized in one of three ways. An unrealized system is valid: its calls are
intent, and specifications still run against it.

**An extension package.** One line on the system names a package and its major version:

```screenplay
system WarehouseChat
  description "The warehouse team's Slack workspace"
  extension "@cratis/screenplay-slack" version 1
```

An extension package ships two things:

- A **contract**: an ordinary Screenplay fragment that declares the abilities, outcomes and application triggers
  the package provides, and the configuration keys it needs. The compiler reads it like a quoted
  [import](../Documentation/screenplay/imports.md) into the system, so `WarehouseChat.PostMessage` and its inputs
  are checked exactly as if the author had written them. A system that names an extension declares no abilities of
  its own; contract and model cannot drift.
- A **performer**: the code that does the work, loaded by the host (section 6). Screenplay never loads it.

Triggers from the contract are qualified by the system (`WarehouseChat.MessageReceived`) and resolve through the
existing trigger order ([triggers.md](../Documentation/screenplay/triggers.md#how-a-name-resolves)) after declared
triggers. This is how inbound signals, such as messages, button clicks and webhook deliveries, reach reactions,
captures and workflow triggers without new inbound grammar.

**A built-in extension with a realization block.** Some realizations are configuration rather than code. A
system may name a built-in extension and give each ability a block named after it. The block is a
[sub-language](../Documentation/screenplay/sub-languages.md) that the extension's contract defines: Screenplay
parses its lines, keeps them in the syntax tree, and has the extension validate them. REST is the first built-in:

```screenplay
system Carrier
  description "The parcel carrier's booking service"
  extension http version 1
  ability BookPickup idempotent
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
    http
      post "/pickups"
      header "Idempotency-Key" = $call.idempotencyKey
      body
        reference = parcelId
        street = address.street
        postalCode = address.postalCode
      response 201
        trackingNumber = body.tracking
        pickupAt = body.window.start
      response 409 -> NoCapacity
```

The request body and response mappings are [structured values](../Documentation/screenplay/structured-values.md),
never JSON (the project's no-JSON rule). The base address and credentials are configuration (below), not
model.

**Inline or file-backed code.** An ability may carry an `implementation` with a tagged fence or a `file`, the
same wrapper operation phases use. It is a new attachment role, `AbilityEffect`, on the decision 0002 envelope:

```screenplay
system Ledger
  description "The finance department's ledger"
  ability OpenCostCenter idempotent
    projectId ProjectId
    returns
      costCenter CostCenterCode
    implementation
      hint "Use the existing accounting adapter"
      file Adapters/OpenCostCenter.cs
```

The `AbilityEffect` context carries the typed inputs, the idempotency key, the attempt number and the call's
causation. Its result is exactly one of: returned values matching `returns`, a declared outcome, or a failure. A
missing or mixed result is Unsupported, never success, as for reaction effects (decision 0121).

**Configuration.** Each system contributes [environment](../Documentation/screenplay/environments.md) keys under
`systems.<System>.`: the keys an extension's contract declares (with their secret flag), and `baseAddress` and the
authentication keys for `http`. `EnvironmentRequirements` lists them. The model never holds a value.

**Realization metadata.** Which extension and which realization block a system uses does not change what a call
*means*, only how it is carried out. Like a [sink](../Documentation/screenplay/sinks.md) (decision 0146), the
`extension` line and realization blocks are realization metadata: they stay out of the executable semantic model
(ESM) bytes and revision, the compilation exposes them beside the ESM, and a renderer hashes them into its render
input. The contract (abilities, inputs, returns, outcomes, effect class) is in the ESM.

### 3. One verb, `calls`, in every behavior

A call names `<System>.<Ability>`, maps each input with `<input> = <source>`, and handles outcomes in ordered
branches. The values available in a branch are `$result.<field>` for success, `$outcome` for the outcome name and
`$failure.reason` for a failure, matching `$refusal` in reaction refusal branches.

| Branch | Taken when |
| --- | --- |
| `on returned` | The ability succeeded. |
| `on <Outcome>` | The ability answered with that declared outcome. |
| `on failed` | Every allowed attempt failed, or the extension refused the call. |

An ambiguous outcome of a `once` ability takes no branch: the call stops for reconciliation (section 6). An
unhandled outcome or failure leaves the call recorded as failed and visible; it is never dropped silently.

**Reactions** call after the triggering fact is committed. This answers #301, open decision 4: an operation
belongs to a command's transaction and is not allowed in a reaction, but a call is.

```screenplay
reaction PickupBooking
  runs as system
  when OrderPacked
    parcelId
    shippingAddress
    calls Carrier.BookPickup
      parcelId = parcelId
      address = shippingAddress
      on returned
        produces PickupBooked
          trackingNumber = $result.trackingNumber
          pickupAt = $result.pickupAt
      on NoCapacity
        produces PickupUnavailable
          parcelId = parcelId
      on failed
        produces PickupBookingFailed
          reason = $failure.reason
```

Branch productions use the ordinary `produces` rules, including `for` defaulting to the triggering event source,
and they settle as one batch under `reaction batches atomic` (decision 0078). A trigger with `calls` cannot also
carry an attached body (decision 0121 keeps body reactions standalone).

**Workflows** gain a `call` node. Its outlets are `returned`, one per declared outcome, and `failed`; the policy
suffix is the one `action` already has. Mappings sit beside the outlets: an `=` line is a mapping and a `->` line
is an edge.

```screenplay
call BookPickup calls Carrier.BookPickup attempts 5 timeout 30 seconds retry after 1 minute
  parcelId = parcelId
  address = shippingAddress
  returned -> TellCustomer
  NoCapacity -> TryTomorrow
  failed -> Escalate
```

A workflow mapping source is a subject property, a trigger value (`$trigger.<value>`), a literal, or a field
returned by an earlier `call` or `compute` node (`BookPickup.trackingNumber`). The node's returned values are kept
in the run's data, so a `decide` condition and later mappings may read them, but only through a path that is
guaranteed to have passed that node (the binder checks dominance and reports otherwise). An ability whose
contract says `answers later` (section 5) suspends the run like a `wait` and takes `expires after`, adding the
`expired` outlet.

**State machines** gain `entering call`, beside `entering send` and `entering start`:

```screenplay
state Shipped
  entering send NotifyCustomer
  entering call WarehouseChat.PostMessage
    channel = "#dispatch"
    text = "An order left the building"
```

A state machine has no data of its own, so an entering call takes no branches. Mapping sources are the followed
read model's properties and literals. When the outcome matters, start a workflow instead. Entering may run again on
redelivery (decision 0124), so the call's identity is derived from the transition, never from the attempt
(section 6).

**Commands** keep `operation` as their way to reach the outside world before commit: the transactional grouping
with `compensate`. A phase may now say `calls` instead of only describing intent or attaching code:

```screenplay
produces operation OpenProjectAccounts
  uses Ledger
  projectId ProjectId = projectId
  execute
    calls Ledger.OpenCostCenter
      projectId = projectId
  compensate
    calls Ledger.CloseCostCenter
      projectId = projectId
```

An operation may contain more than one `calls` and name more than one system, as already decided for #435. A
command's call results never feed its events: operations run after the events are enrolled (Arc
`ICommandOperation`), and a decision must not depend on a non-deterministic answer. A command that needs an
external answer to decide asks for it in a workflow, or records it through a capture or a Translate slice first.
A call that should happen after the command commits belongs in a reaction to the committed event.

### 4. Inline code in workflows and state machines

Inline code gets two homes beyond those that exist, each with a contract that fits it:

- A workflow **`compute`** node runs a pure body over the subject, trigger values and earlier node results. It
  declares `returns` like an ability and either one `next` outlet or two or more author-named outlets, chosen by the
  body. It is a new attachment role, `WorkflowCompute`, with capability `pure`: the host refuses I/O in it,
  replays it freely, and the reference evaluator runs it through the body-execution provider when one is supplied
  (Unsupported otherwise).

  ````screenplay
  compute Risk
    returns
      score RiskScore
    ```csharp
    return new { score = subject.Total > 10_000 ? 80 : 10 };
    ```
    next -> CheckRisk
  ````

- A state machine **`entering`** line may carry a body instead of `send`, `start` or `call`. It reuses the
  reaction-effect contract (decision 0121, version pair 1,1) over the followed read model: it returns events or
  commands, never both, and does no I/O. I/O goes through `entering call`.

Ability `implementation` bodies (section 2) are the effectful counterpart. Together they keep principle 2 true:
anything with an external effect is a call.

### 5. Answers that come later, and people in other tools

Some abilities answer after the call returns: a question posted in Slack, or an adaptive card in Teams. An
extension contract marks such an ability `answers later`, lists the outcomes its answer can take, and the
extension correlates the answer with the call identity it was given. In a workflow, a `call` to such an ability
suspends the run until the answer or the expiry arrives. In a reaction, `answers later` abilities are refused: a
reaction has no place to wait.

An `approval` node may say where the person is asked:

```screenplay
approval AcceptBackorder by WarehouseClerk via WarehouseChat.AskApproval expires after 1 day
  approved -> PlaceBackorder
  rejected -> CancelOrder
  expired -> Escalate
```

`via` names an `answers later` ability whose outcomes include `approved` and `rejected`. The persona or policy
still decides who may answer; the extension supplies the answering identity and the runtime checks it, so the
chat tool never becomes an authorization bypass.

### 6. Runtime: durable, message-oriented calls on Orleans

Screenplay defines what a call means. Stage owns how it runs, on the Orleans silo it already hosts, through
Choreography. The contract Stage implements:

```text
command commit ─┐    reaction observation ─┐    state machine transition ─┐    workflow node ─┐
                ▼                          ▼                              ▼                   ▼
          call intent, recorded with the fact, offset or run that caused it (an outbox, never in memory)
                                              │
                                              ▼
              call run: a Choreography run keyed by the call identity, one node per call
                                              │  capability "ability:<System>.<Ability>@<contract major>"
                                              ▼
                    system gate grain: per-system concurrency and rate limits from deployment
                                              │
                                              ▼
            extension performer: in the silo for trusted .NET packages, or an extension host
            process over the extension protocol (stateless, scaled independently)
                                              │
                                              ▼
                                REST, Slack, Teams, mail, ...
                                              │
                                              ▼
              outcome message back to the origin: a reaction branch settles, a workflow takes the
              outlet, a state machine records the effect as done, an answer resumes a suspended run
```

- **Call identity** is deterministic: the origin's semantic address plus its causation (the triggering event's
  sequence number, the command's correlation, the transition's event, or the run's node execution). It never
  includes the attempt. It is the Orleans grain key, the deduplication key, and the idempotency key the extension
  receives (`$call.idempotencyKey`). Redelivery of a reaction or a state machine entry finds the existing call
  instead of starting another.
- **Delivery is at least once.** Every hop is a persisted message, so a silo restart resumes in-flight calls.
  With the effect class, that gives: `safe` and `idempotent` calls have their effect once, and a `once` call whose
  attempt may have reached the system stops in `reconciliation` (Choreography's ambiguous outcome). An operator
  resolves it in the Stage workbench by choosing the outcome that happened; that choice is recorded, and the
  origin continues from it.
- **Retries, timeouts and backoff** come from the call site (`attempts`, `timeout`, `retry after` in workflows) or
  the deployment's per-system defaults. Pre-commit command operations are the exception: they run inside the
  command request through Arc's command operations, their failure fails the command, and their compensation follows
  the commit disposition, as operations.md describes. They are not durable messages, which is why post-commit
  effects belong in reactions.
- **Scale and isolation.** Calls, call runs and system gates are grains, so they spread across the cluster. A
  slow or failing system fills its own gate, not the silo. Extension hosts hold no state and scale out on their
  own.
- **Pinning and trust.** A run pins the extension package version and contract hash it started with, so upgrading
  an extension never changes a call in flight (Choreography `CapabilityPin`). Stage loads only extensions the
  deployment allows; naming a package in a model is not a trust decision. A missing, unallowed or mismatched
  extension refuses at admission, through `/stage/semantic/admission`, before anything runs.

The extension protocol itself (transport, manifest format and the performer SDK) is Stage's decision, recorded
there. This record fixes only what it must carry: the capability, the call identity, typed inputs, attempt and
deadline in; returned values, a declared outcome, a failure or `answers later` out.

### 7. Specifications

The reference evaluator never contacts an external system. Specifications state what a call answers and assert
what was called, in the forms specifications already use:

| Line | Meaning |
| --- | --- |
| `given <System>.<Ability> returns` with values | Calls to the ability return these values. |
| `given <System>.<Ability> answers <Outcome>` | Calls to the ability answer with the outcome. |
| `given <System>.<Ability> fails` | Calls to the ability fail. |
| `then calls <System>.<Ability>` with values | The call was made with these inputs. Every call is stated, in order, like `then sends`. |
| `outcome <Node> <outlet>` with values | In a workflow, the outlet a `call` node takes and, for `returned`, its values. |

A call with no `given` takes `on returned`. When an ability declares `returns` and the behavior reads a returned
value, the specification must supply it; otherwise the evaluator reports Unsupported and never invents a value.
The existing operation specification forms (`then operation`, `given operation X fails`, `then compensated`) keep
their meaning, and they apply to an operation's phase calls too.

```screenplay
specification APickupWithoutCapacityIsRecorded
  given Carrier.BookPickup answers NoCapacity
  when append OrderPacked
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
    parcelId = "5b1f0c2e-8a4d-4f7b-9e3c-2d6a1b7c8e90"
  then calls Carrier.BookPickup
    parcelId = "5b1f0c2e-8a4d-4f7b-9e3c-2d6a1b7c8e90"
  then PickupUnavailable
```

### 8. Executable semantic model

The ESM version is numbered at admission (decision 0025) and selected only when a model uses an ability, a
`calls`, a `compute` node or an `entering` body, so existing models keep their bytes and revision.

- `application.systems`: `{ name, abilities: [{ name, effect, inputs, returns?, outcomes, answersLater, realization }] }`,
  where `realization` is `{ kind: extension | code | none, requirementId? }`. The package, version and realization
  blocks are realization metadata (section 2), exposed on the compilation as `SystemRealizations` beside the ESM.
- Call sites: `calls` on a reaction trigger (`{ system, ability, values, branches }`); workflow node kinds `call`
  and `compute` (Choreography Action nodes whose capability is the ability, or the compute requirement); state
  machine `entering` kinds `call` and `body`; `execute` and `compensate` phase calls on command operations.
- Specification members for the new `given` and `then` lines, written last and omitted when empty.

Operations stay syntax-only (`PLAY0268`) until a command-side slice admits them; abilities and calls from
reactions, workflows and state machines do not wait for that.

### 9. Diagnostics

New codes take the next free range at implementation, never reusing one. They cover: an unknown system, ability or
outcome; a call that maps an unknown input or misses a required one; incompatible mapping types; a branch for an
outcome the ability does not declare; a duplicate or unreachable branch; `$result` read in a branch other than
`on returned`; a workflow mapping that reads a node not guaranteed to have run; `answers later` called from a
reaction or a state machine; a `via` ability whose outcomes cannot answer an approval; a system that both names an
extension and declares abilities; an unknown realization block or a block the extension refuses; a `compute` body
in a language the host has not admitted; and (warning) a `once` ability called with `attempts` greater than one.

### 10. Surfaces and other repositories

Per the language-change rule, each slice lands on every surface in this repository in its own pull request: both
compilers and their conformance documents, the binder, printer, repairs and rename, the reference evaluator, the
event model board (calls drawn as external effects leaving a slice, and inbound triggers entering it), MCP tools,
the CLI, Monaco and the VS Code grammar (`ability`, `calls`, `compute`, realization blocks registered as
sub-languages), documentation and samples (`Samples/Invoicing` gains a system with abilities; a new sample shows a
REST system and a chat system across a reaction, a workflow and a state machine).

Other repositories get issues when the slices land:

- `Cratis/AI`: corpus skills for systems, abilities, extensions and calls.
- `Cratis/Stage`: admission, the durable call runtime on Choreography, the `http` built-in, the extension protocol
  and host, the `AbilityEffect` and `WorkflowCompute` roles in the body worker, and reconciliation in the workbench.
- `Cratis/Choreography`: correlation of `answers later` replies with suspended runs, and per-capability gates, if
  Stage cannot build them on the existing wait and throttle seams.
- `Cratis/Arc`: operation phase calls rendered as `ICommandOperation` collaborators (Arc#2887).

## Alternatives considered

- **A separate `integration` or `connector` construct.** Rejected: `system` already is the named external
  dependency, and a second construct would split one concept in two. The technology choice is one line on it.
- **Calling an external system through a command in an "adapter slice".** Rejected: it hides the external effect
  inside a command, implies a transaction that does not exist, and cannot express outcomes or idempotency.
- **Letting arbitrary inline code do I/O in reactions, workflows and state machines.** Rejected by principle 2:
  the runtime could not retry, deduplicate, reconcile or specify an effect it cannot see. Code that does I/O is an
  ability `implementation`, so it gets the same guarantees as every other call.
- **Results that become events automatically.** Rejected by principle 3: events are facts the model names, and an
  implicit `PickupBookedReturned` event would be one nobody designed.
- **Synchronous calls from reactions, on the observer's thread.** Rejected: a slow system would stall the
  observer's partition, and a restart would lose the call. Every call is a message.
- **REST described as a JSON request template.** Rejected: Screenplay has no JSON. The `http` block uses
  structured values.
- **Letting an extension add workflow node kinds.** Not adopted: node kinds are part of the ESM contract (decision
  0124 says they change only by a new ESM version). An extension adds abilities and triggers; `call` is the node.

## Open questions

1. **The verb.** `calls` reads naturally beside `invokes` and `sends`; `asks` is warmer but collides with
   approvals in prose. This record proposes `calls`.
2. **Per-tenant credentials.** Whether `systems.<System>.` keys may vary by tenant, or a system is declared per
   tenant, is a host decision that may need a model hint later.
3. **Outcome payloads.** A declared outcome could carry values (`outcome NoCapacity` with `nextSlot DateTime`).
   Deferred until a sample needs it; adding it is additive.
4. **Captures from extensions.** Whether a capture source can be an extension-provided feed (polling a REST
   collection) belongs with the CDL, not here.

## Delivery order

1. Abilities, effect classes and outcomes on `system`; `calls` in reactions with branches; specification lines;
   reference evaluation; ESM admission. Systems are unrealized intent.
2. Stage's durable call runtime on Choreography for reactions, and code-realized abilities through the body
   worker.
3. Workflow `call` and `compute` nodes, and state machine `entering call` and `entering` bodies.
4. The `http` built-in extension and its realization block.
5. Extension packages: contract import, the extension protocol and host, and the first packages (Slack, Teams).
6. `answers later` abilities and `approval ... via`.
7. Operation phase calls in commands, with Arc rendering.

## Related

0002, 0005, 0025, 0078, 0121, 0124, 0146; Screenplay#301, #307, #309, #435; Stage#176; Arc#2887.
