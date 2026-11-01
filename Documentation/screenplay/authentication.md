---
title: "Authentication"
description: "Declare the identity providers an application signs users in with, in the same Screenplay model as its policies and personas."
---

# Authentication

Policies and personas describe *who may do what* — but an application also has to say *how its users sign in*. The top level `authentication` block declares the identity providers of the application, so the sign-in surface is part of the same declaration as everything else.

Additional typed information about the caller belongs in the separate top-level [identity block](identity.md), not under `authentication`. Both blocks are authoring metadata, not ESM behavior.

## Syntax

```screenplay
authentication
  provider <Name> [name <Alias>]
  ...
```

- `authentication` — top level, alongside concepts, policies and personas. At most one block per document; a second one is a compile error.
- `provider <Name>` — one entry per identity provider, naming which provider it is: `EntraId`, `GitHub`, `Google`, `Apple`, `OpenId`.
- `name <Alias>` — what this provider goes by. Optional, and only needed when the provider alone does not identify it.

## Example

```screenplay
authentication
  provider EntraId
  provider GitHub
  provider Google
  provider Apple
```

## Naming a provider

A generic provider can appear more than once. Two `OpenId` providers are two *different* identity providers, and nothing about the word `OpenId` tells them apart — so give them names:

```screenplay
authentication
  provider OpenId name Partner
  provider OpenId name Supplier
```

A provider must be distinguishable: two entries that resolve to the same name are a compile error. Where no alias is given, the provider name is the name.

## What a provider deliberately does not carry

A provider says *which* identity provider the application signs users in with. It says nothing about how to reach one — no authority, no client id, no secret.

That is not an omission. Authority URLs, client ids and the credentials that go with them are what *running* the application needs to know, not what the application *is*, and they differ per environment while the document does not. A document that carried them would be a different document in test and in production, which is the opposite of what it is for.

Configuration lines under a provider are therefore a compile error rather than something quietly ignored — a document that states them is saying something the language does not express, and hiding that would be worse than reporting it.

A runtime such as Stage resolves the configuration itself, by name. Each provider's `Identity` — its alias when it has one, otherwise its name — is its key, so `provider OpenId name Partner` is configured under `authentication.providers.Partner`, with `clientId` and `clientSecret` (a secret) and, for `OpenId`, an `authority`. The full convention, and what the model and an environment each own, is in [environments](environments.md).

Provider identities are realization metadata, not part of the executable semantic model (ESM). A renderer can read them from a successful syntax compilation result:

```csharp
var parsed = new ScreenplayCompiler().Compile(source);
if (parsed.Success)
{
    var providers = parsed.Value!.Authentication?.Providers;
    // Use each provider's Name, Alias, or Identity to select host configuration.
}
else
{
    // Report parsed.Diagnostics; do not render an invalid document.
}
```

Each `AuthenticationProviderSyntax` has `Name`, optional `Alias`, and `Identity` (alias if present, otherwise name). The [compiler's visitors](visitors.md) can traverse the same syntax.

A renderer that holds only a semantic compilation does not need to parse the source again to know what the environment must supply. `SemanticCompilation.Environment` lists the keys of every provider (and of [onboarding](onboarding.md)), with the ones that must be secrets marked; `EnvironmentRequirements.For(syntax)` does the same from a syntax compilation. Do not infer provider identities from ESM. How the runtime stores the values — a secret store, environment variables, or another host mechanism — is its own business.

An application that lets people sign up or invites them declares [onboarding](onboarding.md) next to `authentication`.
