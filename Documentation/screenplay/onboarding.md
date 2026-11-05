---
title: "Screenplay onboarding setup"
description: "Declare signup, invitations, terms and email templates in Screenplay, then supply the host configuration that carries out onboarding."
---

# Onboarding

Screenplay's `onboarding` block declares how people join an application: through signup or invitations, with any required terms and email templates. Use it after declaring [authentication](authentication.md) and the [personas](personas.md) named by invitations, so the model records who can invite people and which personas newcomers receive.

The application host carries out provisioning, term acceptance and email delivery. The block is authoring metadata, outside the executable semantic model; the reference evaluator does not run it. Supply sender and mail-server settings through [environment bindings](environments.md#onboarding). Use [policies](policies.md) to govern commands and queries separately: an onboarding declaration does not enforce their authorization.

## Syntax

```screenplay
onboarding
  signup open creates tenant
  invite tenant as Owner
  invite member
    by Owner, Administrator
    grant Accountant, InvoiceManager
  legal terms
    file Legal/terms-of-service.md
    file Legal/privacy-policy.md
  offering Starter
  emails
    invitation file Emails/invitation.html
    welcome file Emails/welcome.html
    signupDeclined file Emails/signup-declined.html
    images Emails/Images
```

- `onboarding` — top level, alongside `authentication`. At most one block for the whole application; a second one is a compile error. Every line in the block is optional, and each may appear once.
- It needs an [`authentication`](authentication.md) block. Without one, there is nothing for a newcomer to sign in with, and `onboarding` is a compile error.

## Self-signup

```screenplay
signup open creates tenant
signup open joins tenant
```

With no `signup` line, the model offers no self-signup path; newcomers join by invitation. The host must implement that restriction.

- `signup open creates tenant` — anyone may sign up, and signing up creates a new tenant (organization) for them.
- `signup open joins tenant` — anyone may sign up, and signing up asks to join a tenant that already exists.

## Invitations

```screenplay
invite tenant as Owner
invite member
  by Owner, Administrator
  grant Accountant, InvoiceManager
```

- `invite tenant as <Persona>` — an invitation sent by the application's operator that creates a tenant for the invitee, who becomes the persona in it. One line, no body.
- `invite member` — an invitation into a tenant that exists. `by` lists the personas allowed to send it and `grant` the personas the invitee is given. Both are required.

At most one invitation of each kind. Every persona named by `as`, `by` or `grant` must be declared with [`persona`](personas.md): there are no free-text roles, so an unknown persona is a compile error (`PLAY0831`).

## Legal terms

```screenplay
legal terms
  file Legal/terms-of-service.md
  file Legal/privacy-policy.md
```

This block declares the documents a newcomer must accept. The host must collect acceptance; the declaration does not record it or establish legal compliance. Each `file` is relative to the repository root, following the same convention as [handlers and rules](file-references.md). A missing file produces an attachment warning when files are loaded.

## Offering

```screenplay
offering Starter
```

`offering` names the offering a new tenant starts on. Screenplay has no catalog to validate the name against; the provisioning host resolves it. Omitting the line leaves the offering unspecified in the model, without making a claim about the application's licensing.

## Emails

```screenplay
emails
  invitation file Emails/invitation.html
  welcome file Emails/welcome.html
  signupDeclined file Emails/signup-declined.html
  images Emails/Images
```

A template file for each email onboarding sends, and the folder the templates take their images from:

| Line | Email |
|---|---|
| `invitation file <path>` | The invitation a newcomer receives. |
| `welcome file <path>` | The welcome sent when someone has joined. |
| `signupDeclined file <path>` | Sent when a sign-up is declined. |
| `images <folder>` | The folder holding the images the templates use. |

The host is responsible for template rendering and language selection. An email that is not one of the three above is a compile error (`PLAY0832`). The sender and the mail server are environment values: see [environments](environments.md#onboarding).

## What the block deliberately does not carry

Keep sender addresses, mail-server settings, credentials and prices outside this block. They can differ by environment or customer. Once the declarations are in place, use [environment requirements](environments.md#listing-the-keys-a-model-needs) to list the configuration keys and supply their values through the host.

## Diagnostics

| Code | Meaning |
|---|---|
| `PLAY0826` | The `onboarding` line carries anything beyond the keyword. |
| `PLAY0827` | More than one `onboarding` block. |
| `PLAY0828` | `onboarding` without `authentication`. |
| `PLAY0829` | A line of the block is not one of its directives, or one is malformed or incomplete. |
| `PLAY0830` | The same directive stated twice. |
| `PLAY0831` | An invitation names a persona nothing declares. |
| `PLAY0832` | The `emails` block names an email onboarding does not send. |

See [diagnostics](diagnostics.md) for the full catalog.
