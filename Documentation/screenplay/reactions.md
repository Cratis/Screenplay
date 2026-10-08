# Reactions

A reaction is behavior that runs when something happens — the "if this then that" of a Screenplay. It states
what sets it off and what that sets off in turn: notifications, calls to external systems, follow-up events,
or commands. Reactions live inside `Automation` slices.

## Syntax

```screenplay
reaction <Name>
  [description "<text>"]
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

Both are declarations of *what happens*, not of how — a trigger can state its consequences and still carry a
`file` or an inline block that implements them.

## Refusal branches (syntax-only)

> Refusal branches and `$refusal` values are authoring syntax, not yet executable. Both compilers preserve the syntax; the .NET compiler checks selectors and value scope. Binding refuses these constructs with `PLAY0268`, and MCP reports them as unadmitted. [Decision 0030](https://github.com/Cratis/Screenplay/blob/main/decisions/0030-reaction-refusals-and-redelivery.md) defines their intended behavior for admission.

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

| Selector | Intended refusal category |
| --- | --- |
| `on refused` | Validation or constraint refusal, **not authorization** |
| `on refused by validation` | Property rules, concept rules or `require` |
| `on refused by constraint` | Any append-time constraint refusal |
| `on refused by constraint <Name>` | The named, unambiguously resolved constraint |
| `on refused by authorization` | Explicit unauthorized result, never an opaque policy's Unsupported outcome |

Duplicate selectors and narrower branches after a covering branch produce `PLAY0540`. Bare refusal never shadows authorization. An unresolved named constraint produces `PLAY0542`; a known constraint that cannot target any of the invoked command's produced events also produces `PLAY0540`.

The branch's event mappings may use these String values:

| Value | Meaning | Scope |
| --- | --- | --- |
| `$refusal.reason` | `validation`, `constraint` or `authorization` | Any branch |
| `$refusal.constraint` | Violated constraint name | Only a `by constraint` branch |
| `$refusal.message` | Rejection details verbatim, including an unresolved `$strings.` key | Any branch |

Unknown members, use outside a branch's event mapping and incompatible target types produce `PLAY0541`. Messages are display details, not stable identities. Other mapping inputs follow the trigger's rules; read aliases are not branch inputs. For an event trigger, an omitted production `for` is intended to use the triggering event source. At executable admission, clock and application triggers will require `for`; this destination requirement is not enforced by syntax-only authoring yet.

At admission, a refused command contributes no facts. Handling its refusal will stop the remaining invocations of that trigger; other reactions and cascades from accepted facts will continue. Branch productions remain subject to ordinary append constraints. A rejected branch append cannot be caught by another branch. Unhandled refusals end the scenario, retaining previously accepted facts. These are the accepted design, not current runner behavior.

Infrastructure failures, exceptions, contract errors, Unsupported outcomes and concurrency conflicts are never refusals. There is no `by concurrency` form: transient concurrency must fail and retry, not be acknowledged. Refusal handling promises neither a cascade-wide transaction nor once-only external effects. To describe recovery of an existing observed fact, see [redelivery specifications](specifications.md#redelivery-specifications-syntax-only).

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
  facts remain; direct reaction and capture appends retain their individual append disposition.
- A body in code - a `file` or an inline block - is a target's to run. Reaching one returns `SemanticUnsupported`,
  never a guessed result. Reached opaque reducers also fail closed in v6.

Durable collection fan-out, acknowledgement and delivery guarantees remain outside v6
([#286](https://github.com/Cratis/Screenplay/issues/286)); the reference does not approximate them as success.

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
