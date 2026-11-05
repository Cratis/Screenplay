---
title: "Screenplay authentication providers"
description: "Declare sign-in providers in Screenplay, give each a stable name, and identify the environment settings your application host must supply."
---

# Authentication

Screenplay's top-level `authentication` block declares which identity providers an application uses for sign-in. Use it to name the providers in the model while keeping their credentials in the environment. The application host owns the sign-in integration; declaring a provider does not grant access to commands or queries.

Start by naming the providers below, then supply their [environment bindings](environments.md#authentication-providers). Declare [onboarding](onboarding.md) if people can sign up or receive invitations, and attach [policies](policies.md) with `authorize` to state who may act.

Additional typed information about the caller belongs in the separate top-level [identity block](identity.md), not under `authentication`. Both blocks are authoring metadata, outside the executable semantic model (ESM).

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

Give each provider a stable identity for configuration. When two entries use the same provider kind, aliases distinguish them:

```screenplay
authentication
  provider OpenId name Partner
  provider OpenId name Supplier
```

A provider must be distinguishable: two entries that resolve to the same name are a compile error. Where no alias is given, the provider name is the name.

## What a provider deliberately does not carry

Authority URLs, client ids and credentials belong to the environment because they can change between test and production without changing the application's model. Configuration lines under a provider are a compile error.

Screenplay derives configuration keys from each provider's `Identity`: its alias when present, otherwise its name. For `provider OpenId name Partner`, the keys sit under `authentication.providers.Partner`: `clientId`, `clientSecret` (a secret) and `authority`. The host must supply the values and connect them to its sign-in implementation. The [environment binding convention](environments.md) lists the keys; it does not install or configure an identity provider.

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

A renderer that holds only a semantic compilation does not need to parse the source again to know what the environment must supply. `SemanticCompilation.Environment` lists the keys of every provider (and of [onboarding](onboarding.md)), with the ones that must be secrets marked; `EnvironmentRequirements.For(syntax)` does the same from a syntax compilation. Do not infer provider identities from ESM. The host chooses how to supply the values, such as through a secret store or environment variables.

An application that lets people sign up or invites them declares [onboarding](onboarding.md) next to `authentication`.
