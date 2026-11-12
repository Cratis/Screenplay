---
title: "Screenplay extension contracts: the contract file, the manifest and the lock"
description: "The formal contract of an extension package: the contract.play file and its manifest, how a system references a package, the screenplay.lock file, automatic restore and provider compatibility."
---

# Extension contracts

An extension package carries a `contract.play`: its manifest, and everything the package contributes. It is the only part of a package the Screenplay compiler reads. A model points a [system](systems.md) at a package, and the compiler reads the contract as if the author had written the abilities and triggers on the system. This page is the reference for the contract file, the reference line, the lock and restore. The decision behind it is [decision 0160](https://github.com/Cratis/Screenplay/blob/main/decisions/0160-extension-contract-file-the-lock-and-restore.md), which amends [decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md).

## The contract file

A contract holds exactly one `extension <Name>` declaration. Every package, OCI or npm, ships it as `contract.play` (an OCI artifact at `/extension/contract.play`).

```screenplay
extension Stripe
  display name "Stripe payments"
  description "Card payments with Stripe"
  icon "assets/icon.svg"
  image "assets/checkout.png"
    caption "The payment form on a checkout screen"
  requires stage "^5"
  requires platform web
  activation on demand idle after 10 minutes

  setting publishableKey public
  setting secretKey secret
  setting statementDescriptor

  ability CreatePaymentIntent idempotent
    amount Money
    orderId String
    returns
      clientSecret String

  trigger PaymentSucceeded
    paymentIntentId String

  component PaymentForm
    kind react
    platform web
    placeholder "placeholders/payment-form.html"
    property amount Money
    raises succeeded
      paymentIntentId String
    invokes CreatePaymentIntent

  backend runtime dotnet 9 assembly "Cratis.Extensions.Stripe.dll"
  frontend "frontend/index.js"
```

### The manifest

| Line | Meaning |
| --- | --- |
| `display name "<text>"` | The name a person sees. Required. |
| `description "<text>"` | What the package does. Required. |
| `icon "<path>"` | The package-relative path of the icon that represents the package. Required. |
| `image "<path>"` | A package-relative image for a marketplace. Repeat for more. An optional child `caption "<text>"` describes it. The marketplace comes later; the manifest carries the images now. |
| `requires stage "<range>"`, `requires screenplay "<range>"` | The Stage and Screenplay versions the package works with. |
| `requires platform <name>` | A platform the package supports: `web`, `ios`, `android` or `desktop`. |
| `requires runtime <name> <version>` | A runtime the package needs. |
| `activation always \| on demand [idle after <n> <unit>]` | The default activation of the backend. A system's own `activation` line overrides it. |

The contents (abilities, triggers, components, settings and the backend tier) are read from their declarations and never restated in the manifest. A missing display name, description or icon is `PLAY0999`.

### Settings

`setting <name> [public | secret]` declares a setting a system supplies. `public` may reach a browser. `secret` comes from a named [secret](environments.md#system-settings-and-named-secrets) and never reaches a browser. A setting with neither modifier stays with the backend. A setting that is both is `PLAY1103`, so a secret can never reach a browser. A system that maps a setting the contract does not declare, gives a literal to a `secret` setting, or maps a secret to a `public` setting is reported when the model is bound (`PLAY0995`, `PLAY0996`, `PLAY0997`).

### Abilities and triggers

`ability` is the [ability](systems.md) grammar, unchanged. `trigger <Name>` declares an inbound occurrence with typed payload lines.

Types in a contract resolve in the model's own scope, so a contract that says `Money` needs the model to declare a `Money` concept.

### Components

| Line | Meaning |
| --- | --- |
| `kind react \| mount \| screenplay` | How the component is delivered. Required; anything else is `PLAY1100`. |
| `platform <name> ...` | The platforms it supports; an unknown platform is `PLAY1101`. |
| `placeholder "<path>"` | A static HTML file design tools draw in place of the component. Required for `react` and `mount` (`PLAY1102`). |
| `property <name> <Type>` | A typed property of the component. |
| `raises <signal>` | A signal the component raises, with typed value lines below it. |
| `invokes <Ability>` | An ability the component may call through the host. It must be declared in the contract (`PLAY1104`). |

A placeholder has no JavaScript and no external references: styles are inline and images are `data:` URIs. Hosts render it in a sandboxed frame with scripts off. A placeholder with a script, an inline event handler, a `javascript:` URL or an external reference is `PLAY1113` when the package is checked.

### Backend and frontend

`backend runtime <name> <version> assembly "<file>"` names the backend tier. `frontend "<path>"` names the frontend entry point. A frontend-only package, for example an npm package, declares no backend.

Two abilities, triggers, components or settings with the same name, ignoring case, are `PLAY1105`. Anything else in the file is `PLAY0998`.

## Referencing a package

A system names a package with `extension "<reference>" [version <range>]`:

```screenplay
system Payments
  extension "cratis/stripe" version 2
  setting secretKey from secret StripeSecretKey
```

- Without a version the reference means the latest release. `version 2` means the latest 2.x, `version 2.3` the latest 2.3.x, `version 2.3.1` exactly that release, `version "^2.3"` from 2.3 below 3 and `version "~2.3"` from 2.3 below 2.4.
- A reference is a catalog short name (`"cratis/stripe"`), a full OCI reference (`"ghcr.io/acme/extension-ledger"`) or an npm package for frontend-only packages (`"npm:@acme/charts"`).
- The catalog is a naming convention, not a service: `owner/name` resolves to `ghcr.io/owner/extension-name`. Only [restore](#the-cache-vendoring-and-restore) resolves a reference.

A system that names an extension declares no abilities of its own, so the contract and the model cannot drift (`PLAY1106`). When the model is bound the system gets the contract's abilities, and the contract's triggers become application triggers qualified by the system, after the declared ones. A reaction uses one as `when Payments.PaymentSucceeded`. The printed source is unchanged: nothing from the contract is written into your files.

## The lock

`screenplay.lock` sits beside the model root. Commit it. It is plain text, not JSON, and one entry pins each reference:

```text
lock 1

extension "cratis/stripe"
  range "^2"
  resolved "ghcr.io/cratis/extension-stripe"
  version 2.3.1
  digest sha256:<64 hex digits>
  contract sha256:<64 hex digits>
  publisher "https://github.com/cratis"
```

| Field | Meaning |
| --- | --- |
| `extension` | The reference exactly as the model writes it. |
| `range` | The version range the entry was resolved for; `latest` when the model names none. |
| `resolved` | The full reference the name resolved to. |
| `version` | The resolved version. |
| `digest` | The digest of the resolved package. Everything uses it. |
| `contract` | The SHA-256 of the exact bytes of `contract.play`. |
| `publisher` | The identity of the publisher. |

Entries are ordered by reference so a diff shows only real changes. Naming a package in a model is not trusting it: the publisher identity in the lock is what a host checks. A malformed lock is `PLAY1111`.

## The cache, vendoring and restore

The compiler never resolves a range. It reads each contract by the locked digest from two places, in order: the `extensions` folder beside the lock (vendored, committed), then the user cache. Both are laid out `<digest with ':' as '-'>/contract.extension`. The user cache is the folder in `SCREENPLAY_EXTENSION_CACHE`, or a folder in the user's local application data. The file is not named `.play`, so a model folder never mistakes a vendored contract for a model file.

| Situation | The compiler and CI | The language server and MCP |
| --- | --- | --- |
| A reference is not in the lock | Error `PLAY1107` | Warning `PLAY1107`; restore pins it |
| The lock was written for another range | Error `PLAY1110` | Warning `PLAY1110` |
| The contract is not restored | Error `PLAY1108` | Warning "extension not restored" until restored |
| The contract differs from the locked hash | Error `PLAY1109` | Error `PLAY1109` |

The language server and MCP restore unresolved references on opening a model root when `SCREENPLAY_EXTENSION_SOURCE` names a package source.

`screenplay restore [<folder>]` pins and restores by hand; see [the command line](tool.md#restore-extensions). OCI and npm fetching is provided by Stage and the Cratis CLI; this tool reads packages from a directory laid out `<full reference>/<version>/contract.play`.

## Provider compatibility

An extension replaces another only when it offers the same abilities, triggers, components, properties and raised signals, with the same typed shape. It may offer more. When a swap leaves a use of something the new extension does not offer, the use is reported at each place: a reaction that listens to a trigger the extension no longer raises is `PLAY1112`, and an invocation of an ability it no longer offers is reported as an unknown ability. The same comparison is available as `ExtensionCompatibility.Mismatches`.

## Related

- [Systems and abilities](systems.md) declares abilities in a model.
- [Environments](environments.md#system-settings-and-named-secrets) covers settings, named secrets and activation.
- [Grammar](grammar.md) holds the productions.
- [Diagnostics](diagnostics.md#extension-contracts-and-the-lock) lists the codes.
