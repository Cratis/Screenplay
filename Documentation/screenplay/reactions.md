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
    [produces <EventType> ...]
    [invokes <Command> ...]
    [file <Path>]
    [csharp
      ```
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
states which parts of it matter. Taking a value the occurrence does not carry is reported, because the
document already knows what an event and a declared trigger provide.

## Narrowing which occurrences run it

`where` filters trigger occurrences using the same condition grammar as
[`produces when`](commands.md#the-produces-block) and [`require`](policies.md), with `and`, `or` and
parentheses:

```screenplay
event IssueOpened
  labels String
  priority String

reaction HandleImportantIssue
  when IssueOpened
    labels
    priority
  where priority == "high" or priority == "urgent"
```

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
the reaction appends and nothing can refuse it, while a command is an intent handed to something else that
may still validate and reject it. Using `produces` for both would say those are the same kind of
consequence, and they are not.

Both are declarations of *what happens*, not of how — a trigger can state its consequences and still carry a
`file` or an inline block that implements them.

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
    csharp
      ```
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
