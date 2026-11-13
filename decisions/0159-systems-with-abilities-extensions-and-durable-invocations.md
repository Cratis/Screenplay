---
id: 0159
title: Systems declare abilities, extension packages realize them, and every behavior invokes them durably
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

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Screenplay cannot have a construct for everything, just as it cannot draw everything. A model still has to say
that an order posts to the warehouse channel in a chat tool, books a pickup through a carrier's REST API, takes a
card payment, or asks a manager to approve a backorder. Today it can only half say it:

- `system` and `operation` ([operations.md](../Documentation/screenplay/operations.md), #301) name an external
  system and a command's pre-commit work as **intent**. A `system` has no abilities (#301, open decision 2), so an
  operation describes its work only loosely, and binding refuses both with `PLAY0268`.
- Reactions run attached bodies through the body-execution seam (decision 0121), but only to return events or
  commands. They cannot reach the outside world in a way the model can see, retry or specify.
- Workflows and state machines (decision 0124) send commands, wait and ask for approval. They cannot talk to
  anything that is not a command, and they cannot carry inline code. Their verbs also differ from the one
  reactions use: `sends` and `entering send` beside `invokes`.
- Application `trigger`s and the trigger registry name inbound signals, but nothing connects them to the outbound
  work that caused them, such as a reply to a question posted in a chat tool.
- A screen cannot place anything it does not own. A payment form, a map or a chart has to come from somewhere,
  and a frontend component usually arrives together with the backend that serves it.

Stage already runs workflows and state machines on an Orleans silo through Cratis Choreography. Choreography
resolves a node's work through an `IWorkflowNodePerformer` keyed by a `CapabilityReference` (`id@version`),
pins a provider version per run (`CapabilityPin`), retries with an idempotency key, and stops a run for
reconciliation when an outcome is ambiguous. That seam is the runtime this design needs; it is not yet open to
anything but sending commands and evaluating decisions (`StageWorkflowPerformers`).

The request is one extension story for every behavior (commands, reactions, workflows and state machines), with
inline code as an equal citizen, a durable, message-oriented, Orleans-based runtime in Stage, and a packaging
model that lets anyone build an extension in C#, Kotlin, Go or TypeScript and ship it with its own frontend.

## Decision

The design is three constructs that already have a place in the language, one verb, and one package model:

| Concern | Construct | Analogy in the language today |
| --- | --- | --- |
| *What* the business can ask of the outside world | `ability` blocks on a `system` | A `command`'s typed inputs and `returns` |
| *How* it is done (REST, chat, payments, code, ...) | an `extension` line on the `system`, or an `implementation` on the ability | A `sink` (logical in the model, bound in deployment) and an attachment (decision 0002) |
| *Doing it*, from any behavior | `invokes <System>.<Ability>` | `invokes <Command>` |
| Pure inline code inside a process | a `compute` node in a workflow, an `entering` body in a state machine | A reducer or rule body |
| What an extension ships and how a model finds it | an extension package with a contract and a manifest, named by `extension` and pinned by a lock | An import, pinned like a dependency |

Five principles hold the design together:

1. **The model names the business system and what it can be asked; the extension owns the technology.** The
   model never says SMTP, a chat vendor's API or `HttpClient`. Swapping one chat tool for another changes one
   `extension` line, not a single use.
2. **Code computes; abilities touch the world.** Inline code that only computes runs anywhere, any number of
   times. Anything with an external effect goes through an ability, so it gets an identity, an idempotency key,
   retries, a recorded outcome and a place in specifications.
3. **Outcomes become facts only when the model says so.** An invocation's result is never turned into an event
   implicitly. A branch says which event records which outcome, just as reaction refusal branches do.
4. **Naming a package is not trusting it.** A model references an extension; a lock pins what the reference
   resolved to; the deployment decides whether that package may run.
5. **One contract, several channels and languages.** An extension is described by one formal, documented
   contract, whether it travels as an OCI artifact or an npm package and whatever language its backend uses.

### 1. A `system` declares abilities

`system` stays the outbound counterpart of `trigger` (#301). It gains `ability` blocks: a typed contract with
inputs, an optional `returns` block, named business `outcome`s and an effect class. Owned by #1250.

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
  Outcomes are the external counterpart of `on refused by constraint <Name>`. An outcome carries no payload in
  this version, and its name cannot be a reserved outlet (`next`, `refused`, `failed`, `returned`, `expired`).
- **`answers later`** marks an ability whose answer arrives after the invocation returns (section 5).
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

A system says how its abilities are realized in one of three ways. An unrealized system is valid: its invocations
are intent, and specifications still run against it.

**An extension package.** One line on the system references a package, with an optional version:

```screenplay
system Payments
  description "Card payments"
  extension "cratis/stripe"

system WarehouseChat
  description "The warehouse team's chat workspace"
  extension "ghcr.io/acme/extension-chat" version 2
```

A system that names an extension declares no abilities of its own; the contract and the model cannot drift.
Packages, their manifest, their references and their lock are section 3. Realization by an extension is owned by
#1253.

**A built-in extension with a realization block.** Some realizations are configuration rather than code. A
system may name the built-in `http` extension and give each ability a block named after it. The block is a
[sub-language](../Documentation/screenplay/sub-languages.md) that the extension's contract defines: Screenplay
parses its lines, keeps them in the syntax tree, and has the extension validate them. REST is the first built-in
(Cratis/Stage#1069):

```screenplay
system Carrier
  description "The parcel carrier's booking service"
  extension http
  ability BookPickup idempotent
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
    http
      post "/pickups"
      header "Idempotency-Key" = $invocation.idempotencyKey
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
never JSON (the project's no-JSON rule). The base address and credentials are configuration (section 4), not
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

The `AbilityEffect` context carries the typed inputs, the idempotency key, the attempt number and the
invocation's causation. Its result is exactly one of: returned values matching `returns`, a declared outcome, or a
failure. A missing or mixed result is Unsupported, never success, as for reaction effects (decision 0121).

**Realization metadata.** Which extension, version, setting and activation override a system uses does not change
what an invocation *means*, only how it is carried out. Like a [sink](../Documentation/screenplay/sinks.md)
(decision 0146), the `extension` line, realization blocks, `setting` and `activation` lines are realization
metadata: they stay out of the executable semantic model (ESM) bytes and revision, the compilation exposes them
beside the ESM, and a renderer hashes them into its render input. The contract (abilities, inputs, returns,
outcomes, effect class) is in the ESM.

### 3. Extension packages

An extension is one package that can contribute abilities, inbound triggers, frontend components, settings and a
backend. Owned by #1253 (language and contract), Cratis/Stage#1064 (loading) and Cratis/cli#575 to #578
(tooling).

**Two channels, one contract.**

- A **full extension** is one **OCI artifact** holding the contract (`/extension/contract.play`), an optional
  backend in any supported language, an optional frontend (`/extension/frontend/`) and optional
  Screenplay-authored UI (`/extension/ui/*.play`). It is the only way to ship a backend.
- A **frontend-only component package** may instead be an **npm** package. It has no backend, so it needs no
  process.

Both implement the same formal, documented contract; the contract file and its manifest fields are specified in
#1253 and documented in #1256.

**The contract is Screenplay, not JSON.** `contract.play` declares the manifest and everything the package
contributes. The exact keywords are settled in #1253; this shape shows what it must be able to say:

```screenplay
extension Stripe
  display name "Stripe payments"
  description "Card payments with Stripe"
  icon "assets/icon.svg"
  image "assets/checkout.png"
    caption "The payment form on a checkout screen"
  requires stage "^5"
  activation on demand idle after 10 minutes

  setting publishableKey public
  setting secretKey secret

  ability CreatePaymentIntent idempotent
    amount Money
    orderId String
    returns
      clientSecret String

  trigger PaymentSucceeded
    paymentIntentId String

  component PaymentForm
    kind react
    platform web
    placeholder "placeholders/payment-form.html"
    property amount Money
    raises succeeded
      paymentIntentId String
    invokes CreatePaymentIntent

  backend runtime dotnet 9 assembly "Cratis.Extensions.Stripe.dll"
  frontend "frontend/index.js"
```

**The manifest** is part of the contract. It carries a display name, a description, an **icon**, **images** with
optional captions for a marketplace (the marketplace is not yet planned; the manifest carries the images now), the
package's contents (read from the declarations, never restated) and its requirements (compatible Stage and
Screenplay versions, platforms, runtime). A setting is `public` or `secret`, never both, so a secret can never
reach a browser.

**Placeholders.** Each component with a frontend declares a `placeholder`: a static HTML file that design tools
such as Studio draw in place of the component while designing. It has no JavaScript and no external references
(styles inline, images as `data:` URIs), and hosts render it in a sandboxed frame with scripts off.

**Referencing a package.** `extension "<reference>" [version <range>]` on a system.

- The version is optional; without it the reference means the latest release. `version 2` means the latest 2.x,
  `version "^2.3"` a range.
- A reference is a **catalog short name** (`"cratis/stripe"`), a **full OCI reference**
  (`"ghcr.io/acme/extension-ledger"`) or **npm** for frontend-only packages (`"npm:@acme/charts"`).

**The lock.** A plain-text lock file beside the model root, committed and not JSON, pins what each reference
resolved to: the version, the digest, the contract hash and the publisher identity. Everything downstream uses the
locked digest. Its name and format are settled in #1253.

**Automatic restore.** The compiler reads contracts from a digest-keyed local cache. The language server and MCP
restore unresolved references automatically and report "extension not restored", not unknown-name errors, until
they have. The compiler and CI restore from the lock and fail on a reference missing from it; they never resolve a
range themselves. Restoring, updating and vendoring by hand are CLI commands (Cratis/cli#578).

**Trust.** Stage loads only packages the deployment allows: an allow-list of publishers and references, starting
from the project's `.cratis/` configuration and narrowable by the deployment, with signature verification against
the publisher recorded in the lock. Naming a package in a model is never a trust decision. Private registries
authenticate with credentials from configuration, never from the model or the lock (Cratis/Stage#1064).

**Provider compatibility.** An extension replaces another only when it offers the same abilities, components,
properties and raised signals. The compiler checks this and reports a mismatch at each use. A model can therefore
swap a payment provider without silently changing what it may ask of one.

**Triggers.** Triggers from a contract are qualified by the system (`Payments.PaymentSucceeded`) and resolve
through the existing trigger order ([triggers.md](../Documentation/screenplay/triggers.md#how-a-name-resolves))
after declared triggers. This is how inbound signals, such as messages, button clicks and webhook deliveries,
reach reactions, captures and workflow triggers without new inbound grammar (Cratis/Stage#1068).

### 4. Configuration, secrets and activation

Owned by #1254 and Cratis/cli#577.

- **Implicit settings.** A setting the model does not mention is read from configuration under
  `systems.<System>.<setting>`, following [environments.md](../Documentation/screenplay/environments.md). The
  model never holds a value.
- **Explicit secrets.** `secret <Name>` (top level, optional `description`) and `setting <name> from secret <Name>`
  let one secret serve several systems and be described. The deployment binds the name to its store. A literal
  `setting <name> = <value>` is only for non-secret settings, and the environment may override it.
- **A secret never reaches the browser.**
- `EnvironmentRequirements` lists every implicit key and named secret once, with its secret flag.
- **Activation.** The contract states a default: on demand (the default) or always on, with an idle timeout. The
  consumer may override it on the system with `activation always | on demand [idle after <n> <unit>]`.

### 5. One verb, `invokes`, in every behavior

`invokes` is the one verb for asking something outside the current behavior to do work, whether the target is a
command or a `<System>.<Ability>`. Owned by #1251 (the verb) and #1252 (abilities in every behavior). The
reasoning the grammar already gives for reactions holds everywhere: it is an intent handed on that something else
may still reject, fail or answer in a way the author must handle.

| Today | After |
| --- | --- |
| `action <Node> sends <Command>` | `action <Node> invokes <Command>` |
| `entering send <Command>` | `entering invokes <Command>` |
| `entering start <Workflow>` | `entering starts <Workflow>` |
| `then sends <Command>` | `then invokes <Command>` |

`sends`, `entering send`, `entering start` and `then sends` are deprecated: the old forms raise `PLAY0397` with a
quick fix, and the printer writes the new forms. A separate workflow node for abilities does not exist; that
role folds into `action`. UI actions keep `execute`, and `produces` stays for facts.

**Outlets.** An `action` has the outlets `next`, `refused` (commands only; today a refused command surfaces as
`failed`), one outlet per declared outcome (abilities) and `failed`, plus `expired` for an `answers later`
ability that takes `expires after`. The ESM stores the target, not the verb, so the verb change alone leaves bytes
unchanged; the `refused` outlet is an additive member.

An invocation maps each input with `<input> = <source>` and handles results in ordered branches. The values
available in a branch are `$result.<field>` for success, `$outcome` for the outcome name and `$failure.reason`
for a failure, matching `$refusal` in reaction refusal branches.

| Branch (reaction) | Taken when |
| --- | --- |
| `on returned` | The ability succeeded, or the command returned a response. |
| `on <Outcome>` | The ability answered with that declared outcome. |
| `on refused` | The command was refused (commands). |
| `on failed` | Every allowed attempt failed, or the extension refused the invocation. |

An ambiguous outcome of a `once` ability takes no branch: the invocation stops for reconciliation (section 7). An
unhandled outcome or failure leaves the invocation recorded as failed and visible; it is never dropped silently.

**Reactions** invoke after the triggering fact is committed. This answers #301, open decision 4: an operation
belongs to a command's transaction and is not allowed in a reaction, but an invocation is.

```screenplay
reaction PickupBooking
  runs as system
  when OrderPacked
    parcelId
    shippingAddress
    invokes Carrier.BookPickup
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
and they settle as one batch under `reaction batches atomic` (decision 0078). A trigger with `invokes` cannot also
carry an attached body (decision 0121 keeps body reactions standalone). An `answers later` ability is refused in a
reaction: a reaction has no place to wait.

**Workflows** use `action`. Mappings sit beside the outlets: an `=` line is a mapping and a `->` line is an edge.

```screenplay
action BookPickup invokes Carrier.BookPickup attempts 5 timeout 30 seconds retry after 1 minute
  parcelId = parcelId
  address = shippingAddress
  next -> TellCustomer
  NoCapacity -> TryTomorrow
  failed -> Escalate
```

A workflow mapping source is a subject property, a trigger value (`$trigger.<value>`), a literal, or a field
returned by an earlier node (`BookPickup.trackingNumber`). Returned values are kept in the run's data, so a
`decide` condition and later mappings may read them, but only through a path guaranteed to have passed that node
(the binder checks dominance and reports otherwise).

**State machines** use `entering invokes`:

```screenplay
state Shipped
  entering invokes NotifyCustomer
  entering invokes WarehouseChat.PostMessage
    channel = "#dispatch"
    text = "An order left the building"
```

A state machine has no data of its own, so an entering invocation takes no branches. Mapping sources are the
followed read model's properties and literals. When the outcome matters, start a workflow instead. Entering may
run again on redelivery (decision 0124), so the invocation's identity is derived from the transition, never from
the attempt (section 7).

**Commands** keep `operation` as their way to reach the outside world before commit: the transactional grouping
with `compensate`. A phase may now say `invokes` instead of only describing intent or attaching code:

```screenplay
produces operation OpenProjectAccounts
  uses Ledger
  projectId ProjectId = projectId
  execute
    invokes Ledger.OpenCostCenter
      projectId = projectId
  compensate
    invokes Ledger.CloseCostCenter
      projectId = projectId
```

An operation may contain more than one `invokes` and name more than one system, as already decided for #435. A
command's invocation results never feed its events: operations run after the events are enrolled (Arc
`ICommandOperation`, Cratis/Arc#3792), and a decision must not depend on a non-deterministic answer. A command
that needs an external answer to decide asks for it in a workflow, or records it through a capture or a Translate
slice first. Work that should happen after the command commits belongs in a reaction to the committed event.

### 6. Inline code, answers that come later, and approvals

**Inline code** has two homes beyond those that exist, each with a contract that fits it (#1252):

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

- A state machine **`entering`** line may carry a body instead of `invokes` or `starts`. It reuses the
  reaction-effect contract (decision 0121, version pair 1,1) over the followed read model: it returns events or
  commands, never both, and does no I/O. I/O goes through `entering invokes`.

Ability `implementation` bodies (section 2) are the effectful counterpart. Together they keep principle 2 true:
anything with an external effect is an invocation.

**Answers that come later.** Some abilities answer after the invocation returns: a question posted in a chat
tool, or an approval card. A contract marks such an ability `answers later` and lists the outcomes its answer can
take, and the extension correlates the answer with the invocation identity it was given (Cratis/Choreography#200).
In a workflow, an `action` that invokes it suspends the run like a `wait` and takes `expires after`, adding the
`expired` outlet.

**Approvals.** An `approval` node may say where the person is asked:

```screenplay
approval AcceptBackorder by WarehouseClerk via WarehouseChat.AskApproval expires after 1 day
  approved -> PlaceBackorder
  rejected -> CancelOrder
  expired -> Escalate
```

`via` names an `answers later` ability whose outcomes include `approved` and `rejected`. The persona or policy
still decides who may answer; the extension supplies the answering identity and the runtime checks it, so the
chat tool never becomes an authorization bypass.

### 7. Runtime: durable, message-oriented invocations on Orleans

Screenplay defines what an invocation means. Stage owns how it runs, on the Orleans silo it already hosts, through
Choreography (Cratis/Stage#1067). The contract Stage implements:

```text
command commit ─┐    reaction observation ─┐    state machine transition ─┐    workflow node ─┐
                ▼                          ▼                              ▼                   ▼
    invocation intent, recorded with the fact, offset or run that caused it (an outbox, never in memory)
                                              │
                                              ▼
          invocation run: a Choreography run keyed by the invocation identity, one node per invocation
                                              │  capability "ability:<System>.<Ability>@<contract major>"
                                              ▼
                    system gate: per-system concurrency and rate limits from deployment
                                              │
                                              ▼
         extension backend, activated on first need (section 8), over the extension protocol (section 9)
                                              │
                                              ▼
                                REST, chat, payments, mail, ...
                                              │
                                              ▼
       outcome message back to the origin: a reaction branch settles, a workflow takes the outlet,
       a state machine records the effect as done, an answer resumes a suspended run
```

- **Invocation identity** is deterministic: the origin's semantic address plus its causation (the triggering
  event's sequence number, the command's correlation, the transition's event, or the run's node execution). It
  never includes the attempt. It is the Orleans grain key, the deduplication key, and the idempotency key the
  extension receives (`$invocation.idempotencyKey`). Redelivery of a reaction or a state machine entry finds the
  existing invocation instead of starting another.
- **Delivery is at least once.** Every hop is a persisted message, so a silo restart resumes in-flight
  invocations. With the effect class, that gives: `safe` and `idempotent` invocations have their effect once, and
  a `once` invocation whose attempt may have reached the system stops in `reconciliation` (Choreography's
  ambiguous outcome). An operator resolves it in the Stage workbench by choosing the outcome that happened; that
  choice is recorded, and the origin continues from it.
- **Retries, timeouts and backoff** come from the use (`attempts`, `timeout`, `retry after` in workflows) or the
  deployment's per-system defaults. Pre-commit command operations are the exception: they run inside the command
  request through Arc's command operations, their failure fails the command, and their compensation follows the
  commit disposition, as operations.md describes. They are not durable messages, which is why post-commit effects
  belong in reactions.
- **Scale and isolation.** Invocations, invocation runs and system gates are grains, so they spread across the
  cluster. A slow or failing system fills its own gate, not the silo.
- **Pinning.** A run pins the extension's locked digest and contract hash it started with, so upgrading an
  extension never changes an invocation in flight (Choreography `CapabilityPin`).
- **Admission.** A missing, unallowed, unsigned or mismatched extension, or one whose requirements are not met, is
  refused through `/stage/semantic/admission` before anything runs.
- **Refusal is distinct from failure.** A workflow action that invokes a command reports `refused` separately from
  `failed`.

### 8. Execution tiers, lazy activation and frontend components

Owned by Cratis/Stage#1066 (tiers and activation), Cratis/Scene#296 and Cratis/Stage#1070 (components), and #1255
(using components on screens).

**Execution tiers.** The contract declares the tier, and the deployment, not the extension, decides which
extensions share a host, so fifty extensions do not mean fifty always-on containers.

| Tier | Process layout |
| --- | --- |
| None | No process (frontend-only or declarative extensions) |
| Shared runtime host | One host per runtime (`dotnet`, `node`, `jvm`) loads many extensions, isolated within it |
| Native binary | A small per-extension process |
| Dedicated container | Its own image and process |

**Lazy activation, like Orleans grains.** A backend starts on its first need and stops after its idle timeout.
Durable invocations wait in the outbox, so a cold start is invisible to them. The extension's default (on demand
or always on) can be overridden by the consumer (section 4). Stage knows which screens place which extension
components, so a screen that uses one lets Stage activate its backend before the first `invoke` from the browser
(frontend-triggered activation).

**Frontend component kinds over one contract.** Two kinds are first-class and a third is Screenplay-authored:

- `react`: a React component rendered in the host's own React tree, sharing the host's React and composing with
  Scene components. Web only.
- `mount`: `mount(host, properties, context) => { update(properties), unmount() }`, a framework-neutral floor for
  Vue, Svelte, Lit, web components or plain DOM.
- `screenplay`: components authored in Screenplay templates in the package, receiving the same properties and
  context from the renderer.

All three share one contract: typed properties, raised signals and a context. The context carries the user,
tenant, locale, theme, the system's public settings, strings, `invoke`, `raise` and `on`. It reaches React through
`use*()` hooks (`useExtensionContext()`, and focused hooks such as `useUser()` and `useInvoke()`) and reaches mount
components through an argument. A conformance kit runs the same expectations against a component of each kind.

**On a screen.** `component <System>.<Component>` resolves through the system's extension contract; properties
are checked against the contract's typed `property` lines, `on <signal>` handles a signal the contract raises, and
contract triggers are available as `on <System>.<Trigger>`. A component whose platforms do not include the
profile's targets is reported.

### 9. The extension protocol and host services

Owned by Cratis/Stage#1065. Stage hosts backends over a two-way **gRPC** protocol defined in versioned `.proto`
files. Stage asks the extension to describe itself (a handshake that checks the running backend against its
contract), perform an invocation, handle a routed inbound HTTP request and report health. The extension asks Stage
for common services: settings and secrets scoped to the system, raising a contract trigger, answering later by
invocation identity, and logging and telemetry tied to the invocation's causation. Every request carries the
tenant, the invocation identity, the idempotency key, the attempt and the deadline. Thin SDKs for C#, Kotlin, Go
and TypeScript sit over the protocol, and every SDK passes one conformance suite.

### 10. Specifications

The reference evaluator never contacts an external system. Specifications state what an invocation answers and
assert what was invoked, in the forms specifications already use:

| Line | Meaning |
| --- | --- |
| `given <System>.<Ability> returns` with values | Invocations of the ability return these values. |
| `given <System>.<Ability> answers <Outcome>` | Invocations of the ability answer with the outcome. |
| `given <System>.<Ability> fails` | Invocations of the ability fail. |
| `then invokes <System>.<Ability>` with values | The invocation was made with these inputs. Every invocation is stated, in order, like `then invokes <Command>`. |
| `outcome <Node> <outlet>` with values | In a workflow, the outlet an `action` node takes and, for `next`, its values. |

An invocation with no `given` takes `on returned`. When an ability declares `returns` and the behavior reads a
returned value, the specification must supply it; otherwise the evaluator reports Unsupported and never invents a
value. The existing operation specification forms (`then operation`, `given operation X fails`,
`then compensated`) keep their meaning, and they apply to an operation's phase invocations too.

```screenplay
specification APickupWithoutCapacityIsRecorded
  given Carrier.BookPickup answers NoCapacity
  when append OrderPacked
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
    parcelId = "5b1f0c2e-8a4d-4f7b-9e3c-2d6a1b7c8e90"
  then invokes Carrier.BookPickup
    parcelId = "5b1f0c2e-8a4d-4f7b-9e3c-2d6a1b7c8e90"
  then PickupUnavailable
```

### 11. Executable semantic model

The ESM version is numbered at admission (decision 0025) and selected only when a model uses an ability, an
ability invocation, a `compute` node, an `entering` body or the `refused` outlet, so existing models keep their
bytes and revision.

- `application.systems`: `{ name, abilities: [{ name, effect, inputs, returns?, outcomes, answersLater, realization }] }`,
  where `realization` is `{ kind: extension | code | none, requirementId? }`. The package, version, settings,
  activation and realization blocks are realization metadata (section 2), exposed on the compilation as
  `SystemRealizations` beside the ESM.
- Invocation sites: `invokes` on a reaction trigger (`{ system, ability, values, branches }`); workflow `action`
  nodes whose capability is the ability, and `compute` nodes whose capability is the compute requirement; state
  machine `entering` kinds `invokes` and `body`; `execute` and `compensate` phase invocations on command
  operations.
- Specification members for the new `given` and `then` lines, written last and omitted when empty.

Operations stay syntax-only (`PLAY0268`) until a command-side slice admits them; abilities and invocations from
reactions, workflows and state machines do not wait for that.

### 12. Diagnostics

New codes take the next free range at implementation, never reusing one. They cover: an unknown system, ability or
outcome; an invocation that maps an unknown input or misses a required one; incompatible mapping types; a branch
or outlet for an outcome the ability does not declare; a duplicate or unreachable branch; `$result` read outside
`on returned`; a workflow mapping that reads a node not guaranteed to have run; `answers later` invoked from a
reaction or a state machine; a `via` ability whose outcomes cannot answer an approval; a system that both names an
extension and declares abilities; an unknown realization block or a block the extension refuses; a `compute` body
in a language the host has not admitted; a setting that is both `public` and `secret`; a frontend component without
a placeholder; a component invoking an ability the contract does not declare; an extension swapped for one that
is not compatible; a reference missing from the lock; and (warning) a `once` ability invoked with `attempts`
greater than one.

### 13. Surfaces and other repositories

Per the language-change rule, each slice lands on every surface in this repository in its own pull request: both
compilers and their conformance documents, the binder, printer, repairs and rename, the reference evaluator, the
event model board (invocations drawn as external effects leaving a slice, and inbound triggers entering it), MCP
tools, the CLI, Monaco and the VS Code grammar (`ability`, `invokes`, `compute`, `extension`, realization blocks
registered as sub-languages), documentation and samples (`Samples/Invoicing` gains a system with abilities; a new
sample shows a REST system and a chat system across a reaction, a workflow and a state machine).

| Part of the design | Owner |
| --- | --- |
| This record | #1249 |
| Abilities, outcomes, effect classes | #1250 |
| The `invokes` verb | #1251 |
| Invocations in every behavior, `compute`, `entering` bodies, approvals | #1252 |
| Contracts, manifests, the lock, restore, provider compatibility | #1253 |
| Settings, secrets, activation override | #1254 |
| Extension components on screens | #1255 |
| Documentation and the cross-repository map | #1256 |
| Loading, trust, admission, asset serving | Cratis/Stage#1064 |
| Protocol, host services, backend SDKs | Cratis/Stage#1065 |
| Execution tiers and activation | Cratis/Stage#1066 |
| Durable invocations | Cratis/Stage#1067, Cratis/Choreography#200 |
| Inbound traffic | Cratis/Stage#1068 |
| The built-in `http` extension | Cratis/Stage#1069 |
| Component contract and hosting | Cratis/Scene#296, Cratis/Stage#1070 |
| CLI: create, verify, restore, configure | Cratis/cli#575, #576, #578, #577 |
| Templates | Cratis/Templates#104 |
| Operation phases in commands | Cratis/Arc#3792 |
| Corpus skills | Cratis/AI#1121 |

The epic is #1248.

## Alternatives considered

- **A separate `integration` or `connector` construct.** Rejected: `system` already is the named external
  dependency, and a second construct would split one concept in two. The technology choice is one line on it.
- **A separate verb for abilities (`asks`, `runs`) and a separate workflow node for them.** Rejected: the language
  already has `invokes` for handing work on, and a third verb beside `invokes` and `sends` would make authors
  choose between synonyms. One verb and one `action` node mean a command and an ability read alike and share
  outlets, policies and specifications.
- **Invoking an external system through a command in an "adapter slice".** Rejected: it hides the external effect
  inside a command, implies a transaction that does not exist, and cannot express outcomes or idempotency.
- **Letting arbitrary inline code do I/O in reactions, workflows and state machines.** Rejected by principle 2:
  the runtime could not retry, deduplicate, reconcile or specify an effect it cannot see. Code that does I/O is an
  ability `implementation`, so it gets the same guarantees as every other invocation.
- **Results that become events automatically.** Rejected by principle 3: events are facts the model names, and an
  implicit `PickupBookedReturned` event would be one nobody designed.
- **Synchronous invocation from reactions, on the observer's thread.** Rejected: a slow system would stall the
  observer's partition, and a restart would lose the work. Every invocation is a message.
- **REST described as a JSON request template.** Rejected: Screenplay has no JSON. The `http` block uses
  structured values.
- **Letting an extension add workflow node kinds.** Not adopted: node kinds are part of the ESM contract (decision
  0124 says they change only by a new ESM version). An extension adds abilities, triggers and components;
  `action` is the node.
- **npm as the only channel.** Rejected: npm cannot carry a backend in Kotlin or Go, a contract and a frontend
  together with it, or an identity Stage can verify against an allow-list. npm stays for frontend-only packages.
- **One always-on container per extension.** Rejected: fifty extensions would mean fifty processes. Tiers and lazy
  activation let the deployment share hosts and start backends only when needed.
- **Trusting a package because a model names it.** Rejected: a model is authored content. Trust comes from the
  deployment's allow-list and signatures, and the lock records what was reviewed.
- **Only one frontend component kind.** Rejected: React alone excludes teams on other frameworks, and a mount
  function alone loses native React composition. Both are first-class over one contract.
- **Provider compatibility that tolerates differences.** Rejected: a swap that silently drops an ability or a
  signal breaks a model without warning. The compiler requires the same surface and reports each use.

## Scope

In scope: the language constructs, the contract and manifest, the lock and restore, the verb change, the runtime
contract Stage implements, the frontend component contract, the tooling and the documentation, in the repositories
listed in section 13. Out of scope: the marketplace that shows manifest images, a catalog service (who runs it is
open), billing or licensing of extensions, and any extension implementation beyond the built-in `http`.

## If this is never accepted

The language keeps two verbs for handing work on, `system` stays a name with no abilities, and every integration
is hand-written code outside the model, invisible to specifications, retries and the event model board. The cost
is the one this record exists to remove: each integration is built again, with its own idempotency and failure
handling, and the model cannot say what the business asks of the outside world.

## Timeline

The record has to be accepted before any slice in #1248 starts, because each depends on it. The delivery order is
below. The verb change (#1251) is small and independent, so it may land as soon as the record is accepted.

## Open questions

Resolved by the work that landed under #1248. The resolutions are recorded here; the decision itself is unchanged.

1. **Stage services in version one.** Resolved. Version one has `GetSetting`, `GetSecret`, `RaiseTrigger`, `Answer`, `Log`
   and `RecordMetrics`. A key-value state store and an outbound credential proxy are deliberately left out and reserved
   for a later minor version of the protocol (Cratis/Stage#1065, Stage `Documentation/extensions/index.md`). The
   credential proxy needs a declarative way to name the hosts an extension may reach, which belongs in the contract and
   is not yet specified.
2. **Where the SDKs live.** Resolved. The protocol and the backend SDKs live in the Cratis/Stage repository
   (Cratis/Stage#1065).
3. **Who runs the catalog** of short names, and the **lock's name and format**. Resolved. The catalog is a naming
   convention, not a service: `owner/name` resolves to `ghcr.io/owner/extension-name`. The lock is the plain-text
   `screenplay.lock` (decision 0160, #1253).
4. **Activation trigger.** Resolved. Reaching a screen that places a system's components activates its backend;
   mounting a component does not, and the first call still activates a backend that nothing else started
   (Cratis/Stage#1066, Stage `Documentation/extensions/execution-tiers.md`).
5. **Per-tenant credentials.** Open. Whether `systems.<System>.` keys may vary by tenant, or a system is declared per
   tenant, is a host decision that may need a model hint later.
6. **Outcome payloads.** Open. A declared outcome carries no payload in this version. Adding values (`outcome NoCapacity`
   with `nextSlot DateTime`) is additive and waits for a sample that needs it.
7. **Captures from extensions.** Open. Whether a capture source can be an extension-provided feed (polling a REST
   collection) belongs with the CDL, not here.
8. **The marketplace.** Open. It is not yet planned; the manifest carries the images now.

## Delivery order

1. This record (#1249), then the verb change (#1251).
2. Abilities (#1250), invocations in every behavior (#1252) and configuration (#1254) in the language, with the
   corpus issue (Cratis/AI#1121) alongside each.
3. Stage's durable invocation runtime (Cratis/Stage#1067) and Choreography (Cratis/Choreography#200), with
   code-realized abilities.
4. Packages: contracts and the lock (#1253), restore (Cratis/cli#578), loading (Cratis/Stage#1064), the protocol
   and SDKs (Cratis/Stage#1065) and tiers (Cratis/Stage#1066).
5. Frontends: Cratis/Scene#296, #1255 and Cratis/Stage#1070; inbound traffic (Cratis/Stage#1068) and the `http`
   built-in (Cratis/Stage#1069).
6. Tooling and templates (Cratis/Templates#104, Cratis/cli#575, #576, #577), operations in Arc (Cratis/Arc#3792),
   and the documentation map (#1256) last.

## Verification

**Done when:** every issue in #1248 is closed; both compilers and the reference evaluator accept abilities,
`invokes` and extension references with held-equal conformance documents; Stage runs an invocation durably from a
reaction, a workflow and a state machine, restores a package from the lock and refuses an unallowed one at
admission; and the documentation in each repository links the others.

**Verify by:** the conformance documents and canonical vectors pass for the new constructs; the samples compile
with no deprecated syntax; a Stage integration run shows a `once` invocation stopping for reconciliation and an
`idempotent` one retrying with the same key; and an admission request for an unlocked or unsigned package is
refused.

## Consequences

The language gains one verb and a way to name abilities, and authors lose the second verb with a quick fix. Models
that do not use abilities or extensions keep their ESM bytes and revisions. Stage gains a durable invocation
runtime, an extension loader with a trust model, and a frontend component host, which are real operational
surface and are specified in their own repositories. Extensions become a published contract that third parties
build against, so changing it later costs a version.

## Related

0002, 0005, 0025, 0078, 0121, 0124, 0146; Screenplay#301, #307, #309, #435, #1248; Stage#176; Arc#2887.
