---
title: "Onboarding"
description: "Declare how people get into an application: self-signup, invitations, legal terms, an offering and the emails sent along the way."
---

# Onboarding

An application with [authentication](authentication.md) can sign people in, but the model also has to say how people *get in*: whether they can sign themselves up, who can invite whom, which documents they must accept and what the emails they receive look like. The top level `onboarding` block declares that, in the same model as the personas it refers to.

The block is authoring metadata. It does not enter the executable semantic model, and the reference evaluator does not run it: provisioning is carried out by whatever runs the application. Everything the block needs from the world outside the model — the address mail is sent from, the mail server and its password — is an [environment value](environments.md), never part of the model.

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

Self-signup is **off** unless the block says otherwise: with no `signup` line, nobody can sign themselves up and every newcomer arrives by invitation.

- `signup open creates tenant` — anyone may sign up, and signing up creates a new tenant (organization) for them.
- `signup open joins tenant` — anyone may sign up, and signing up asks to join a tenant that already exists.

## Invitations

```screenplay
invite tenant as Owner
invite member
  by Owner, Administrator
  grant Accountant, InvoiceManager
```

- `invite tenant as <Persona>` — an invitation that creates a tenant for the invitee, who becomes the persona in it. One line, no body.
- `invite member` — an invitation into a tenant that exists. `by` lists the personas allowed to send it and `grant` the personas the invitee is given. Both are required.

At most one invitation of each kind. Every persona named by `as`, `by` or `grant` must be declared with [`persona`](personas.md): there are no free-text roles, so an unknown persona is a compile error (`PLAY0831`).

## Legal terms

```screenplay
legal terms
  file Legal/terms-of-service.md
  file Legal/privacy-policy.md
```

The documents a newcomer has to accept. When the block is present, acceptance is required. Each `file` is relative to the repository root, the same convention [handlers and rules](file-references.md) use, and a file that cannot be found is reported the way a missing handler file is: as an attachment warning when files are loaded.

## Offering

```screenplay
offering Starter
```

Optional. The name of the offering a new tenant starts on. The name is opaque to the model — Screenplay has no catalog to check it against — and is resolved where the application is provisioned. Without an `offering`, no licensing is involved.

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

Languages follow the cultures of the model's `.strings` files. An email that is not one of the three above is a compile error (`PLAY0832`). The sender and the mail server are environment values: see [environments](environments.md#onboarding).

## What the block deliberately does not carry

No sender address, no mail server, no credentials, no price. Those differ per environment (or per customer) while the model does not, so the model refers to them by name and the environment supplies them.

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
