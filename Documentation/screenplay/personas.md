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
- Each `policy` line references a policy declared in the same file — an unknown policy is reported as a warning.
- The description, when present, is the first body line and appears at most once.

## Guidance

- **Name personas after roles, not people** — `Accountant`, not `Alice`.
- **Personas group policies; policies define rules.** A persona says *who*; its policies say *what they must satisfy*.
