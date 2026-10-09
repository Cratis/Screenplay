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
- Each `policy` line references a declared policy — an unknown policy is an error in compilation and executable binding, so an unresolved persona cannot be mistaken for anonymous access. A Draft authoring workspace may retain it as reported reference debt, but is not executable until the policy resolves.
- The description, when present, is the first body line and appears at most once — a quoted single line or a fenced multi-line block (see [Descriptions](slices.md#descriptions)).

Personas remain authoring metadata, outside the executable semantic model (ESM). In a [specification](specifications.md), `given caller as Accountant` expands into an ordinary caller before binding; the persona name does not enter the ESM. The caller is always authenticated and satisfies every persona policy. Its canonical bytes and revision match an explicit caller stating the same roles and claims.

## Persona callers in specifications

The deterministic witness collects required authenticated, role and literal-string-claim atoms first, across all policies. It then visits policies in declared order, depth first, left to right. An `or` already satisfied by those atoms adds nothing; otherwise it chooses the leftmost buildable alternative. **Operand order matters**: the `InvoiceManager` above gets only the `InvoiceManager` role, while `Accountant` already has the required `Accountant` role and needs no extra role.

Roles are de-duplicated and sorted ordinally. Claims are de-duplicated by type (ordinal ignoring case) and value (ordinal), then sorted in that order. This specification proves the outcome for one deterministic minimal witness, not for every caller fitting the persona.

Synthesis refuses a persona without policies, any policy containing `not`, and inline or file implementation policies. A subject, path or `$` claim target refuses synthesis only when needed. Claims whose type equals `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`, ordinal ignoring case, are never synthesized; a bare `"role"` claim type is not this URI. An `or` skips an alternative needing an unsupported claim and reports the first unbuildable alternative's refusal if none is buildable. Use an explicit `given caller` for these cases; refusal is a binding error, never a guessed runtime allow or deny.

Hover and MCP `declaration-details` with `kind: "Persona"`, `view: "caller"` show the synthesized caller, its policy contributions, or the refusal. `find-fixtures` reports persona origins. The opt-in [completeness](completeness.md) check `personas` reports unused personas, commands or queries definitely denied to every synthesized persona, and unpinned buildable alternatives. Ordinary compilation does not run it.

## Guidance

- **Name personas after roles, not people** — `Accountant`, not `Alice`.
- **Personas group policies; policies define rules.** A persona says *who*; its policies say *what they must satisfy*.
