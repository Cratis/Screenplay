---
title: Why Screenplay
description: The friction Screenplay removes — one model instead of several hand-synchronized layers — and an honest look at where a hand-written slice is still the better fit.
---

You already know what the feature is before you write a line of it. On the whiteboard there is a command, the events it produces, a read model those events project into, and a screen that shows the read model. The design is done. What remains is the part nobody enjoys: writing that same design out several more times, in several different places, and keeping every copy in step for the rest of the project's life.

## The friction: one model, written four times

A single "register an invoice" feature, built by hand, is spread across four artifacts that all describe the *same* thing:

- the **C# command** and its validation and authorization,
- the **events** it appends,
- the **projection and read model** that turn those events into queryable state,
- the **React screen** that renders it — through generated proxies that have to be regenerated whenever the backend shifts.

Nothing keeps them honest with each other. Rename a field on the event and the projection, the proxy, and the screen quietly fall out of sync until something breaks at runtime. The model in your head — the thing you actually reasoned about — exists nowhere as a single artifact. It has been shredded across layers.

## The relief: describe it once

A Screenplay `.play` file *is* that model, written down once:

```screenplay
slice StateChange RegisterInvoice
  command RegisterInvoice
    invoiceId     InvoiceId
    invoiceNumber InvoiceNumber
    authorize CanManageInvoice
    validate
      invoiceNumber matches "^INV-[0-9]{6}$"  message "Must look like INV-000000"
    produces InvoiceRegistered
      invoiceId     = invoiceId
      invoiceNumber = invoiceNumber
      registeredAt  = $context.occurred

  event InvoiceRegistered
    invoiceId     InvoiceId
    invoiceNumber InvoiceNumber
    registeredAt  DateTime
```

The command, its rules, and the fact it records sit together in one place, in the order you reasoned about them. Screenplay now owns a versioned portable semantic foundation, and the compiler and downstream products are being connected to it capability by capability. Stage, Studio, and generated applications may not redefine that meaning or silently ignore a capability they cannot preserve.

```mermaid
flowchart LR
    subgraph by_hand["By hand — four copies to keep in sync"]
      direction TB
      WB["🧠 the model<br/>(on the whiteboard)"] --> CS["C# command + events"]
      CS --> PX["generated proxies"]
      PX --> UI["React screen"]
    end
    subgraph screenplay["With Screenplay — one source of truth"]
      direction TB
      Play["📄 one .play model"] --> Stage["🎬 runtimes perform it"]
      Play --> Studio["🎨 tools visualize it"]
    end
    by_hand -.->|"collapses into"| screenplay
```

## What makes it hold together

Three design choices keep the single file both complete and honest — they are covered in depth in the [language overview](overview.md):

- **Slices are the atom.** Every construct lives inside a typed slice aligned with Event Modeling's vocabulary, so the file's structure *is* the model's structure — not a technical layering of it.
- **Concepts carry compliance.** A value type declares `@pii` or `@sensitive` once, and every place that value appears inherits it. Compliance stops being a per-field decision you can forget.
- **Declarative first, with bounded escape hatches.** The business meaning stays declarative. Selected implementation points can carry constrained inline code or a `file` reference without turning code into the semantic authority.

## When a hand-written slice is the better fit

Screenplay is not always the right tool, and pretending otherwise would waste your time:

- **Your system is not naturally command- and fact-oriented.** Screenplay is grounded in Event Modeling, event sourcing, commands, views, and vertical slices. It should not be forced onto a problem that has no useful expression in those terms.
- **Almost every construct needs custom logic.** The escape hatch is there for the hard 10%. If a slice is 90% inline C#, the declarative wrapper is adding ceremony rather than removing it — write that slice by hand.
- **You need behavior the portable model cannot yet express.** Do not hide that gap behind a large code block or assume every runtime behaves the same way. Keep the implementation explicit and contribute evidence for the smallest missing semantic capability.

Convinced it fits? [Write your first `.play` file →](./getting-started.md)
