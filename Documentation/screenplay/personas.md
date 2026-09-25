# Personas

Personas name the roles that interact with the application — the vocabulary of Event Modeling's actors. A persona has a name, an optional description, and the [policies](policies.md) that define what the persona is allowed to do.

## Syntax

```screenplay
persona <Name>
  [description "<text>"]
  [policy <PolicyName>]*
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

## Rules

- Personas are top-level declarations, alongside policies and modules.
- Each `policy` line references a declared policy — an unknown policy is an error, so an unresolved persona cannot be mistaken for anonymous access.
- The description, when present, is the first body line and appears at most once — a quoted single line or a fenced multi-line block (see [Descriptions](slices.md#descriptions)).

Personas are report-only authoring metadata in the current executable semantic model (ESM). A valid persona no longer blocks binding, but neither its name nor its policies are added to the ESM or enforced by its evaluator. Consumers that need the declaration must use the parsed syntax; an ESM-only renderer cannot recover it.

## Guidance

- **Name personas after roles, not people** — `Accountant`, not `Alice`.
- **Personas group policies; policies define rules.** A persona says *who*; its policies say *what they must satisfy*.
