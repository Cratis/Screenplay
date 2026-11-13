---
title: "Personas"
description: "Declare Screenplay personas, the named roles that interact with an application and the policies that define what each may do."
---

# Personas

Use a persona to name a role that interacts with your application, such as `Accountant` or `InvoiceManager`. These are the actors in Event Modeling. Give each persona a name, an optional description, and the [policies](policies.md) that define what it is allowed to do.

## Syntax

```screenplay
persona <Name>
  [description "<text>"]
  [goal "<text>"]*
  [policy <PolicyName>]*
  [position <x> <y>]
  [size <width> <height>]
```

## Example

```screenplay
policy IsAccountant
  require role "Accountant"

policy CanManageInvoice
  require role "InvoiceManager" or role "Accountant"

persona Accountant
  description "Keeps the books and approves invoices"
  policy IsAccountant
  policy CanManageInvoice

persona InvoiceManager
  description "Registers invoices and manages their lifecycle"
  policy CanManageInvoice
```

Both personas above use `CanManageInvoice`. `Accountant` also requires `IsAccountant`, so the shared policy does not make their requirements identical. The persona groups those requirements; each policy defines the rule.

## The personas canvas

Add `goal` lines to record what a persona is trying to achieve. Tools that draw persona cards can also use `position <x> <y>` and `size <width> <height>` to preserve your arrangement. Position coordinates and size dimensions are whole numbers in canvas units; positions may be negative, sizes may not. Without a position, the canvas lays out the persona.

Use a sticky note for context about a persona, such as a pain point or an open question. Notes are top-level declarations, separate from the personas themselves. The same kind of note is available on the event source, given context and seed data canvases as `sticky note on eventsources`, `sticky note on givencontexts` and `sticky note on seeddata`; see [event sources](event-sources.md).

```screenplay
persona Shopper
  description "Browses the catalog and buys"
  goal "Find the right product quickly"
  position 0 0
  size 300 180

sticky note on personas
  description "Pain point: the checkout asks for too much"
  colour pink
  position 340 0
  size 220 140
```

A sticky note needs a `description` (the text on it, a quoted line or a fenced block) and a `position`; `colour` is one of `yellow`
(the default and never printed), `pink`, `green` or `blue`, and `size` is optional. Goals, positions, sizes and notes never enter the
executable semantic model: its bytes and revision are unchanged by them.

## Rules

- Personas are top-level declarations, alongside policies and modules.
- Each `policy` line references a declared policy — an unknown policy is an error in compilation and executable binding, so an unresolved persona cannot be mistaken for anonymous access. A Draft authoring workspace may retain it as reported reference debt, but is not executable until the policy resolves.
- The description, when present, is the first body line and appears at most once — a quoted single line or a fenced multi-line block (see [Descriptions](slices.md#descriptions)).

Personas remain authoring metadata, outside the executable semantic model (ESM). In a [specification](specifications.md), `given caller as Accountant` expands into an ordinary caller before binding; the persona name does not enter the ESM. The caller is always authenticated and satisfies every persona policy. Its canonical bytes and revision match an explicit caller stating the same roles and claims.

## Persona callers in specifications

For `given caller as Accountant`, Screenplay builds one concrete caller that satisfies the persona's policies. This deterministic witness first collects the required authentication, roles and literal-string claims across all policies. It then visits policies in declared order, depth first, left to right. An `or` already satisfied by those requirements adds nothing; otherwise it chooses the leftmost buildable alternative.

Operand order therefore matters. The `InvoiceManager` above gets only the `InvoiceManager` role, while `Accountant` already has the required `Accountant` role and needs no extra role. Roles are de-duplicated and sorted ordinally. Claims are de-duplicated by type (ordinal ignoring case) and value (ordinal), then sorted in that order. The specification proves the outcome for this one deterministic minimal witness, not for every caller fitting the persona.

Synthesis refuses a persona without policies, any policy containing `not`, and inline or file implementation policies. A subject, path or `$` claim target refuses synthesis only when needed. Claims whose type equals `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`, ordinal ignoring case, are never synthesized; a bare `"role"` claim type is not this URI. An `or` skips an alternative needing an unsupported claim and reports the first unbuildable alternative's refusal if none is buildable. Use an explicit `given caller` for these cases; refusal is a binding error, never a guessed runtime allow or deny.

Hover and MCP `declaration-details` with `kind: "Persona"`, `view: "caller"` show the synthesized caller, its policy contributions, or the refusal. `find-fixtures` reports persona origins. The opt-in [completeness](completeness.md) check `personas` reports unused personas, commands or queries definitely denied to every synthesized persona, and unpinned buildable alternatives. Ordinary compilation does not run it.

## Users

A `user` is a person a development or test session can act as. It is declared on one line, names the personas it plays, and
holds whatever roles and claims those personas' policies give it.

```screenplay
user kari "Kari Nordmann" as Accountant, InvoiceManager
user ola "Ola Nordmann" as Accountant
user guest "Guest"
```

The name is the identity a session presents; the quoted display name is optional and the name stands in without it. A user
that plays no persona is authenticated and holds no roles, which is how to test what a signed-in person without access sees.

- A user plays only declared personas; an unknown one is `PLAY0888`, and a name used twice is `PLAY0889`.
- Users, like goals and positions, never enter the executable semantic model: its bytes and revision are unchanged by them,
  and a model without users compiles exactly as before.
- Users exist for tooling that exercises a running application: [Stage](https://github.com/Cratis/Stage) offers them to
  Lens when its own development mode and its explicit users switch are both on, and
  never in a published or rendered application.

## Audiences

A persona can carry `audience <Name>` lines naming the [audiences](availability.md) it is in. They are for authoring only: the board, Studio and documentation use them to show what the persona sees, and they change nothing at runtime.

## Guidance

Name personas after roles, not people: `Accountant`, not `Alice`. Put the rules in policies so the model keeps the role and its requirements distinct.
