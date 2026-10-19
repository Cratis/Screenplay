---
title: "Reactions"
description: "Declare Screenplay reactions, the if-this-then-that behavior that runs when an event or trigger occurs in an Automation slice."
---

# Reactions

A reaction is behavior that runs when something happens — the "if this then that" of a Screenplay. It states
what sets it off and what that sets off in turn: notifications, calls to external systems, follow-up events,
or commands. Reactions live inside `Automation` slices.

A reaction can carry one nonempty fenced `markdown` `documentation` block directly in its body to explain its assumptions and recovery ownership. It is authoring-only (`PLAY0270`), with no executable effect; it does not belong under a trigger. See [Descriptions and documentation](slices.md#descriptions-and-documentation).

## Syntax

```screenplay
reaction <Name>
  [description "<text>"]
  [runs as system [role "<Role>" { and role "<Role>" }]]
  <trigger>
    [description "<text>"]
    [<value> ...]
    [reads <View> [as <alias>] [by <trigger value>] ...]
    [produces <EventType> ...]
    [invokes <Command> ...]
    [file <Path>]
    [```csharp
      <C# returning event side effects>
      ```]
  [where <condition>]
```

A reaction declares at least one trigger. Everything under a trigger is optional.

## Observer filters

A reaction may declare one leaf `from Source` or `from Source.Stream`. The filter belongs to the reaction, not its trigger. It observes only facts whose stored routing names match. Unrouted facts never match. Stream ids are not filtered. Without `from`, observation is unchanged.

A filtered reaction uses only `when Event` triggers on declared events. It cannot use clock or host-signal triggers. Invalid filters use `PLAY0665`. The C# compiler warns with `PLAY0666` when every known producer lands outside the filter. Handlers and foreign origins are unknown and suppress that warning. Filters select ESM v10. Projections and captures do not gain metadata filters. See [Event sources](event-sources.md#observer-filters).

Trigger reads may supply composite keys through a child `by` block with named `part = source` mappings. Sources are required, compatible trigger values. Single-instance reads supply every key part. These reads remain authoring-only and have no automatic key repair. See [Read-model keys](readmodels.md#keys).

## Returned-command identity

A gated command needs a caller even when a reaction asks for it. Declare the trusted returned-command path explicitly:

```screenplay
policy ClaimsAutomation
  require role "ClaimsAutomation"

module Claims
  feature Expiration
    slice Automation CloseExpiredClaims
      event ClaimDeadlinePassed
        claim String
      command CloseClaim
        claim String
        authorize ClaimsAutomation
      reaction CloseExpiredClaims
        runs as system role "ClaimsAutomation"
        when ClaimDeadlinePassed
          claim
          invokes CloseClaim
            claim = claim
```

`runs as system` declares an authenticated system identity with exactly the quoted roles; omitting roles is valid. Roles must be nonempty, distinct literals, not expressions. Declare the line once, directly under the reaction, anywhere among its reaction-level directives. The printer places it after documentation and before the first trigger. Personas are not accepted here.

The identity covers every command returned or `invokes` under every trigger. It does not cover imperative `ICommandPipeline` calls inside an inline or `file` implementation, `produces`, `reads`, refusal-branch productions or causation. It is authorization identity, not caller audit identity; `given caller` never supplies an actor to invocations.

Declaring `runs as` selects **released ESM v10**. The reference evaluator authenticates this system principal and grants exactly its declared roles. Every claim condition remains unknown, including under `not`; a satisfied role alternative can allow, but a final unknown denies. This is a deliberate deny-only divergence from Arc: Arc's system principal carries `[System]` subject claims, but the portable contract does not pin those claims. Clock and application triggers may declare the identity, but Stage has no Arc realization for either trigger kind yet. MCP reports that as readiness, not a source diagnostic.

Without the line, invocations retain their no-caller behavior and existing executable bytes. Arc runs returned commands as the system only for a reactor carrying `[ExecuteCommandsAsSystem]`; an unmarked reactor has no principal and authorization gates deny.

Both compilers warn on a gated invocation without identity (`PLAY0648`), or on an identity with neither invocations nor an implementation body (`PLAY0649`). An authorization refusal branch receives `PLAY0557` instead of `PLAY0648`, once per invocation. The C# binder also warns about unused roles (`PLAY0650`, silent for opaque gates or implementation bodies) and definite authorization denial (`PLAY0651`, unknown stays silent). Cross-module roles are valid and listed in MCP details without a diagnostic. Opt into the [privilege completeness check](completeness.md) to inspect who can produce the events that reach this trusted path.

## Trigger → reaction → effects

The model has three parts, and the language gives each its own word:

| Part | What it is |
| --- | --- |
| **Trigger** | something that can cause the reaction to run |
| **Trigger data** | the values that particular occurrence hands the reaction |
| **Reaction** | the behavior that runs, and what it sets off |

A reaction never needs to know where its trigger came from. `when OrderPlaced` reads the same whether
`OrderPlaced` is a domain event from the event store, a signal an integration raises, or something the host
does — that is the trigger's business. See [Triggers](triggers.md) for declaring one and for the built-in set.

## What sets a reaction off

Three forms, and a reaction may declare several:

```screenplay
reaction OrderHandling
  when OrderPlaced
  every 15 minutes
  at 08:00
```

`when <Name>` names an event, a trigger the document declares, or one a consumer registered with the
compiler. `every` and `at` are the clock — spelled the way a schedule is said out loud rather than as a
trigger with arguments, because a reaction driven by the passage of time is common enough to earn the words.

| Form | Runs |
| --- | --- |
| `every 30 seconds` | on that interval |
| `every 15 minutes` | on that interval |
| `every 2 hours` | on that interval |
| `every 1 day` | on that interval |
| `at 08:00` | every day, at that time |
| `at 09:30 on Monday` | every week, on that day |
| `at 00:00 on day 1` | every month, on that day |

A time with no qualifier is every day. A day of the week and a day of the month cannot both be given —
most months have no such occurrence.

## The values a reaction takes

Under a trigger, a bare name says the reaction uses that value from the occurrence:

```screenplay
event OrderPlaced
  order String
  customer String

reaction HandleOrder
  when OrderPlaced
    order
    customer
```

This is a selection, not a declaration — the shape belongs to the event or the trigger, and the reaction
states which parts of it matter. Clock triggers (`every` and `at`) carry no values: a value line under one is an error. Taking a value the occurrence does not carry is reported, because the
document already knows what an event and a declared trigger provide. A value named `reads` must be
written `@reads` under a reaction trigger; the escape keeps it distinct from a view read.

## Views a reaction decides from

A trigger can declare the views it consults before acting:

```screenplay
event OrderPlaced
  orderId Uuid

readmodel OrderStatus
  status String

reaction HandleOrder
  when OrderPlaced
    orderId
    reads OrderStatus as current by orderId
```

`reads <View> [as <alias>] [by <trigger value>]` names a view the trigger reads. It takes no indented children; a child line is an error and is not taken as a trigger value. If a trigger reads
the same view more than once, every read needs a distinct alias; aliases must be unique within the
trigger and must not match a trigger value. `by` must name a value taken by that trigger. Clock triggers (`every` and `at`) take no values,
so they can use `reads <View>` but not `by`. An unknown view is reported. A reaction that invokes a
command leaves the command's decision to that command and its own reads. ESM v6 does not add protected
reaction reads: a trigger with `reads` that produces directly, or has an opaque effect body, fails binding
because its decision dependency cannot be protected. It is not admitted as an unguarded decision. An
invocation-only reaction leaves read protection to the invoked command; it cannot use those views as
portable mapping values.

`for each <View>` is reserved for a future view-driven trigger, not part of this grammar.

## Narrowing which occurrences run it

`where` filters trigger occurrences using the same condition grammar as
[`produces when`](commands.md#the-produces-block) and [`require`](policies.md), with `and`, `or` and
parentheses:

```screenplay
event IssueOpened
  labels String
  title String

reaction HandleImportantIssue
  when IssueOpened
    labels
    title
  where labels contains "important" or title starts with "URGENT"
```

Alongside `==`, `!=` and the ordering operators, text compares with `contains` for a substring anywhere and
`starts with` for one at the beginning.

`where` belongs to the reaction rather than to one trigger: it says which occurrences are worth running
for, whatever set them off.

## A trigger states intent on its own

Screenplay's workflow is *author the document first, then Stage performs it*, so a reaction must be
describable before any code exists. A trigger with nothing under it is already a complete statement — this
reaction runs when that happens:

```screenplay
reaction PaymentReconciler
  description "Matches settled payments against outstanding invoices and closes them out"
  when InvoicePaid
  when InvoiceMarkedOverdue
    description "Re-checks whether a late payment has since arrived"
```

That document parses, and it tells a reader exactly what the reaction is for — with no file to point at and
no code to invent. The `file` reference and the inline block are
[realization metadata](grammar.md#declarative-first--file-is-never-required), attached once the slice is
implemented.

Give the reaction a `description` for what it does overall, and a trigger its own `description` when *that*
reaction needs explaining beyond the trigger's name.

## What the reaction sets off

An automation is the "if this, then that" of the system, and a document that could only draw the *if* left
the arrows out of an automation invisible.

```screenplay
event InvitationAccepted
  workspaceId Uuid

event WorkspaceProvisioned
  workspaceId Uuid

command SendWelcomeMail
  workspaceId Uuid

reaction Provisioner
  when InvitationAccepted
    workspaceId
    produces WorkspaceProvisioned
      for workspaceId
      workspaceId = workspaceId
    invokes SendWelcomeMail
      workspaceId = workspaceId
```

`produces` is the same declaration a [command](commands.md#the-produces-block) carries — appending an event
is the same act wherever it happens, including `for` to say which event source it lands on.

**`invokes` is a different word on purpose.** A command is not produced; it is asked for. An event is a fact
the reaction asks to append, subject to append-time constraints, while a command is an intent handed to
something else that may authorize, validate and reject it. Using `produces` for both would say those are the same kind of
consequence, and they are not.

For an event-triggered reaction, map an invoked command's identifier with `orderId = $eventSourceId`
(or `$eventContext.eventSourceId`) to address the triggering event's source without copying its identity into
the event payload. The same source is available in reaction `produces` mappings. It needs a required scalar
target, not a clock, caller or audit identity; clock and application triggers have no event source.
The reference runner validates the source value against the mapping target's type without converting it.
These mappings use the existing ESM v6 reaction contract.

Both are declarations of *what happens*, not of how — a trigger can state its consequences and still carry a
`file` or an inline block that implements them.

## Recovery redelivery

Use `when redelivered <Event> to <Reaction>` to deliver one existing given fact to one event-trigger reaction again. ESM v17 establishes givens without running reactions, fires only the named observer (respecting its `from` and `where` filters), and settles any cascade from newly appended facts. It never appends the given fact again. `then no events` asserts no effects, while event expectations compare all newly accepted facts. An unhandled validation or constraint refusal remains assertable with `then error`. See [Redelivery specifications](specifications.md#redelivery-specifications) for locator rules and examples. This is ordinary observation, not replay or a once-only external-effect guarantee.

## Refusal branches

Refusal branches and `$refusal` values select ESM v19 and execute in the reference runner under [decision 0030](https://github.com/Cratis/Screenplay/blob/main/decisions/0030-reaction-refusals-and-redelivery.md). Both compilers preserve the syntax; the .NET compiler binds it and checks executable scope and destinations. Consumers must explicitly admit v19.

Inside `invokes`, state how the reaction should handle a command's refusal. This excerpt assumes the command, constraint and output event are declared:

```screenplay
invokes Claim
  invoice = invoice
  on refused by constraint UniqueClaim
    produces Refused
      reason = $refusal.reason
      constraint = $refusal.constraint
      message = $refusal.message
  on refused by validation
    acknowledge
  on refused by authorization
    acknowledge
```

Branches are ordered: the first matching selector wins. A branch contains `acknowledge` alone or one or more ordinary `produces <Event>` blocks, with mappings and optional `for`. Empty branches, repeated acknowledgement, acknowledgement combined with productions, operations, inline event declarations and implementation attachments are invalid.

| Selector | Refusal category |
| --- | --- |
| `on refused` | Validation or constraint refusal, **not authorization** |
| `on refused by validation` | Error-severity property rules, concept rules or `require` |
| `on refused by constraint` | Any append-time constraint refusal |
| `on refused by constraint <Name>` | Any violated constraint with the named, unambiguously resolved identity |
| `on refused by authorization` | Explicit unauthorized result, never an opaque policy's Unsupported outcome |

Duplicate selectors and narrower branches after a covering branch produce `PLAY0540`. Bare refusal never shadows authorization. An unresolved named constraint produces `PLAY0542`; a known constraint that cannot target any of the invoked command's produced events also produces `PLAY0540`.

Both compilers warn with `PLAY0557` on an `on refused by authorization` branch when the invoked command has an `authorize` gate of its own or inherits one from its module or enclosing features, but the reaction has no declared `runs as`. The reference runner has no caller and always denies a gated command. Arc runs commands as the system only for a reactor carrying `[ExecuteCommandsAsSystem]`; an unmarked reactor also has no principal. Declare `runs as system role "<Role>"` to state the intended identity; this suppresses the warning and selects ESM v10 for system authorization. Refusal branches themselves select ESM v19. `given caller` does not supply that identity. Validation and constraint branches do not receive `PLAY0557`; a gated invocation without an authorization branch receives `PLAY0648` instead.

The branch's event mappings may use these String values:

| Value | Meaning | Scope |
| --- | --- | --- |
| `$refusal.reason` | `validation`, `constraint` or `authorization` | Any branch |
| `$refusal.constraint` | First violated constraint name in result order | Only a `by constraint` branch |
| `$refusal.message` | Messages of the classified kind joined with `\n` in result order, verbatim; authorization uses its denial reason | Any branch |

Unknown members, use outside a branch's event mapping and incompatible target types produce `PLAY0541`. Messages are display details, not stable identities. Other mapping inputs follow the trigger's rules; read aliases are not branch inputs. For an event trigger, an omitted production `for` uses the triggering event source. Binding requires `for` on clock and application triggers. Messages retain whitespace and unresolved `$strings.` keys.

The first matching branch wins. Classification gives authorization precedence over constraint, then validation. A refused command contributes no facts. Handling its refusal stops the remaining invocations of that trigger; other reactions and cascades from accepted facts continue. Branch productions remain subject to ordinary append constraints. A rejected branch append ends the scenario and cannot be caught by another branch. Unhandled refusals end the scenario, retaining previously accepted facts.

Infrastructure failures, exceptions, contract errors, Unsupported outcomes and concurrency conflicts are never refusals. There is no `by concurrency` form: transient concurrency must fail and retry, not be acknowledged. Refusal handling promises neither a cascade-wide transaction nor once-only external effects. To describe recovery of an existing observed fact, see [redelivery specifications](specifications.md#redelivery-specifications).

## Atomic production batches

Declare `reaction batches atomic` at the application root to prevent a rejected later production from leaving an accepted prefix. Imported reactions inherit this mode. It selects ESM v22 and the optional final application member `reactionProductionMode: "atomic"`. Duplicate or conflicting declarations report `PLAY0693`; unknown modes are invalid.

One trigger occurrence's direct `produces` list is one batch. Each selected invocation-refusal branch is another batch; invoked commands keep their own atomic appends. Another trigger or occurrence is never silently combined with it. All candidates are materialized and append-validated before private projections and publications are prepared. Primary facts are adopted first, followed by derived publications, and only adopted facts start cascades. The complete expanded result must fit the 1,000-new-fact budget before adoption. Given history does not consume that budget.

A failed batch leaves its pre-batch facts, read models and pending children unchanged. Earlier accepted batches and commands remain. The first violation is selected by authored production order, then ordinal constraint name; `SemanticRejected.Production` carries a zero-based index, event contract and route without changing the authored message. Property-value reservations made inside a batch are not freed by a source changing value or releasing; the same source may repeat its own claim. Unique-event cycles do honor a valid release before a later claim.

Without the declaration, **per-fact production is legacy behavior kept for byte stability**, even in a newer ESM version. Recompilation never opts an existing model in. New models should declare the mode. Atomic batches do not admit direct-production stream routes or closing constraints (#770), deduplicate redelivery, promise atomic observer snapshots, or recover an unknown storage outcome. Consumers must explicitly admit v22 or refuse it.

## In the executable semantic model

Reactions bind to the executable semantic model as ESM v6 ([decision 0022](https://github.com/Cratis/Screenplay/blob/main/decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)),
so a specification can assert what a reaction does. In the reference evaluator:

- After every accepted fact, the reactions to its event run: their `produces` first, then their `invokes`,
  in the order the trigger states them. Facts are reacted to in the order they were appended.
- A `produces` without `for` appends to the event source of the event that set the reaction off. A clock,
  application or built-in trigger has no such event, so what it produces needs `for` - a value it carries, or
  literal text.
- An `invokes` runs the command through its full pipeline - authorization, validation, requirements and
  constraints - with no caller. A command that requires one rejects the reaction, and the rejection ends
  the scenario. Previously accepted facts remain in the world; a failing invoked command appends none of
  its own facts. There is no transaction around the entire cascade.
- In ESM v7, an invoked command that only returns a response runs; the evaluator computes its response
  and discards it. The response does not become the initiating command's response.
- Invocation of a command with generated properties binds in ESM v7, but an invocation that reaches
  generation returns `Unsupported(IdentityAllocation)`, because invocation has no generation fixture
  channel. A branch excluded by `where` does not reach generation.
- Invocation does not turn the command identifier into an allocated destination. Legacy plain `produces`
  without `for` still requires an explicitly supplied allocation, which this invocation profile does not
  provide; it returns typed `IdentityAllocation` unsupported instead of guessing.
- Reactions to one fact run by semantic identity, and triggers within a reaction run in authored order.
  Occurrence time propagates through the cascade. The invoking reaction is recorded as causation, not
  as a caller audit identity. Each effect checks its audit requirements when reached; a later unsupported
  effect does not discard earlier accepted facts or replace an earlier rejection. Bodies excluded by `where` do not run.
- `where` guards every trigger of the reaction. Each trigger must resolve the complete condition from its
  declared scalar values, whether or not those values are listed in the reaction's input selection. Missing
  operands, nested paths and read aliases fail binding rather than silently removing the guard. Supported
  comparisons, enumeration constants and logical groups use the command condition contract.
- The scenario budget counts new accepted facts, not established history. A command batch that would exceed
  1,000 facts is unsupported before any of its facts or projection changes enter the world. Prior accepted
  facts remain; legacy direct reaction and capture appends retain their individual append disposition. Explicit atomic reaction batches reserve their complete expanded result before adoption.
- A body in code - a `file` or an inline block - is a target's to run. Reaching one returns `SemanticUnsupported`,
  never a guessed result. Reached opaque reducers also fail closed in v6.

Durable collection fan-out, acknowledgement and delivery guarantees remain outside v6
([#286](https://github.com/Cratis/Screenplay/issues/286)); the reference does not approximate them as success.

### Settle reactions in a live host

Use `SemanticReactionSettlement` after `SemanticEvaluator.Execute` accepts a command, before persisting its
facts, to run event-triggered reactions and their cascades through invoked commands. It shares the
specification runner's reaction semantics, including `from`, `where`, append order and the 1,000-fact limit.
The limit counts the observed input facts as well as reaction effects, not established history.

Pass the accepted command result's `World` and `Facts`. That world already contains the command's facts and
any projection changes from them. Facts that other writers appended outside a command enter the world through
[`ExtendWorld`](projections/semantic-model.md#extending-a-world-with-facts-from-outside), before the command is
committed; see the host loop below.

An accepted settlement contains the settled world and **only the additional reaction facts**, in append
order, with no queries or response. Append the command's facts followed by the settlement's facts together
in one batch; settlement performs no storage writes. Unlike the specification runner, this API is
all-or-nothing: a rejected, unsupported or conflict result returns the input world without partial reaction
effects. The host must refuse the whole command, appending neither the command's facts nor any reaction
facts. Empty input or no matching reaction accepts the unchanged world with no additional facts.

This host helper takes an admitted plan, your evaluator and the accepted result of `SemanticEvaluator.Execute`:

```csharp
using Cratis.Screenplay.Semantics.Execution;

static SemanticExecutionResult EvaluateReactions(
    ISemanticEvaluator evaluator,
    SemanticExecutionPlan plan,
    SemanticAccepted accepted,
    DateTimeOffset? occurred)
{
    var settlement = new SemanticReactionSettlement(evaluator);

    return settlement.Settle(plan, accepted.World, accepted.Facts, occurred);
}
```

Invoked commands run through the supplied `ISemanticEvaluator` with the reaction's declared `runs as`
identity. Wrap that evaluator to preserve your host's pre-checks on invoked commands too. Treat a rejected,
unsupported or conflict result as failure, not as an empty successful append.

This API does not fire clock, startup, shutdown or application triggers, and it does not execute captures.
A reached opaque reaction body returns `SemanticUnsupported`; it is still the target provider's to run.

### Append a settled command in a live host

A host that writes to a store runs the command in five steps, ending in a guarded batch write
([decision 0089](https://github.com/Cratis/Screenplay/blob/main/decisions/0089-expose-the-reference-evaluators-decide-commit-split-to-hosts.md)):

1. **Decide.** Call `SemanticEvaluator.Decide` on the world you read.
2. **Extend.** Read the log up to its current tail and call `SemanticEvaluator.ExtendWorld` with every fact after the decided world.
3. **Commit.** Call `SemanticDecision.Commit` with the extended world. Only a fact that matches the decision's dependencies conflicts; irrelevant facts are settled against.
4. **Settle.** Run `SemanticReactionSettlement.Settle` on the accepted result.
5. **Write.** Persist the command's facts and the settlement's facts as one batch, guarded on the log holding exactly the extended world's facts.

Two windows can lose a race, and each has its own rule:

| Window | What can happen | Rule |
| --- | --- | --- |
| Decide to commit | Another writer appends before you commit. | Extend with everything up to the tail you read just before persisting, then commit against that world. A matching fact conflicts; an irrelevant one settles against the later world. |
| Settle to write | Another writer appends after you settled. | Write the whole batch atomically, guarded on the **durable pre-batch world**: the extended world you passed to `Commit`, before any tentative root or cascade facts. The log must hold exactly those facts. A successful command with no facts makes the same guarded write, because it still validates its dependencies. |

Commands that settlement invokes decide and commit at one world through `Execute`, so they have no first window.

When the guarded write is definitively not written because the log moved:

- **Settlement invoked no commands.** Retry from step 2 with the same `SemanticDecision`. Extend the **extended world you passed to `Commit`** (not the tentative result, which holds root and cascade facts that were never written) with only the facts the log holds after that world's facts, in log order, then commit the same decision against the result. `ExtendWorld` assumes the log still begins with the world's facts and does not check it; if it may not, rebuild the world with `EstablishWorld` from the log instead. This is sound because `Commit` is pure and the values the decision generated were fixed when `Decide` ran.
- **Settlement invoked a command.** The operation fails as `Conflict` / `DecisionStateChanged`. Do not retry it automatically: re-running would decide the invoked commands again against a new world.
- **The write outcome is unknown.** Neither retry nor report success. Read the log to find out what was written.

The guarded write, the retry bound and the unknown-outcome handling are the host's own; the corpus does not cover
them. The [race vectors](interoperability.md#protected-command-read-race-vectors) cover steps 1 to 3.

## Examples

Delegating to a file:

```screenplay
reaction NotifyCustomer
  description "Emails the billing contact once an invoice is registered"
  when InvoiceRegistered
    file Reactions/NotifyCustomerReaction.cs
```

Inline C#:

````screenplay
reaction OverdueInvoiceDetector
  when InvoiceStatusChanged
    ```csharp
      if (@event.Status != InvoiceStatus.Paid &&
          @event.ChangedAt < DateTimeOffset.UtcNow.AddDays(-30))
      {
          return [new MarkInvoiceOverdue(
              InvoiceId: @event.InvoiceId,
              OverdueAt: DateTimeOffset.UtcNow
          )];
      }
      ```
````

Inside the block, `@event` is the triggering occurrence; returned events are appended as side effects.

## Guidance

- Reactions that only translate events into other events belong in `Translate` slices when driven by
  external data ([captures](captures.md)); event-to-event automation stays in `Automation` slices.
- Keep reaction logic small; anything substantial belongs in a `file` reference where it can be tested on
  its own.
- Describe the reaction before you implement it — a document full of `file` lines and nothing else tells a
  reader nothing.
- A name that only an integration knows belongs in a [`trigger`](triggers.md) declaration, so the document
  says what the reaction is handed rather than leaving the reader to guess.
