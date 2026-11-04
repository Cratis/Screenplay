---
title: "Environments"
description: "How a model's declarations are bound to the configuration of the environment an application runs in, by name, and which values must be secrets."
---

# Environments

A model says what an application *is*. An environment says where one instance of it *runs*: which identity provider's client id it uses, which mail server it sends through. The two stay apart on purpose, and they meet by **name**.

## What belongs where

| In the model | In the environment |
|---|---|
| Which identity providers the application signs people in with (`authentication`) | Each provider's client id, client secret and authority |
| That onboarding sends emails, and which templates it uses | The sender address, the mail server and its credentials |
| Personas, policies, events, screens | Nothing |

A model is the same document in test and in production. A value that differs per environment, and a secret in particular, therefore never appears in the model: configuration lines under a provider are a compile error rather than something quietly ignored. A secret is **referenced by key only**, and the environment decides where its value lives.

## The binding convention

Every key is derived from a declaration, so nothing needs to be repeated or configured twice. Keys are dotted, case-sensitive paths.

### Authentication providers

A provider is found by its `Identity` — its `name` alias when it has one, otherwise the provider name:

| Key | Secret | For |
|---|---|---|
| `authentication.providers.<Identity>.clientId` | no | every provider |
| `authentication.providers.<Identity>.clientSecret` | **yes** | every provider |
| `authentication.providers.<Identity>.tenantId` | no | `EntraId` |
| `authentication.providers.<Identity>.authority` | no | `OpenId` |

```screenplay
authentication
  provider EntraId
  provider OpenId name Partner
  provider OpenId name Supplier
```

needs `authentication.providers.EntraId.clientId`, `authentication.providers.EntraId.clientSecret`, `authentication.providers.EntraId.tenantId`, and the same `clientId`, `clientSecret` and `authority` under `authentication.providers.Partner` and `authentication.providers.Supplier`. Two `OpenId` providers are told apart by their `name`; without one they would both resolve to `OpenId`, which is already a compile error (`PLAY0169`).

### Onboarding

When the model declares [`onboarding`](onboarding.md), it needs the mail settings, because the sender and the mail server are never in the model:

| Key | Secret |
|---|---|
| `onboarding.email.sender` | no |
| `onboarding.email.server.host` | no |
| `onboarding.email.server.port` | no |
| `onboarding.email.server.username` | no |
| `onboarding.email.server.password` | **yes** |

## Listing the keys a model needs

A runtime reads the keys from a successful compilation, so it can report *"environment `production` is missing `authentication.providers.EntraId.clientSecret`"* before it starts, instead of failing at the first sign-in.

From a syntax compilation:

```csharp
var parsed = new ScreenplayCompiler().Compile(source);
if (parsed.Success)
{
    EnvironmentRequirements requirements = EnvironmentRequirements.For(parsed.Value!);
    foreach (var key in requirements.Keys)
    {
        // key.Key, key.IsSecret, key.Source, key.Owner, key.Description, key.Location
    }
}
```

From a semantic compilation, without parsing the source again:

```csharp
var compiled = new SemanticModelCompiler().Compile(applicationName, documents);
if (compiled.Success)
{
    var secrets = compiled.Value!.Environment.Secrets;
}
```

`EnvironmentRequirements` (namespace `Cratis.Screenplay.Environments`, package `Cratis.Screenplay`) holds `Keys` in declaration order and `Secrets`, the keys that must be supplied as secrets. Each `EnvironmentKey` has `Key`, `Source` (`AuthenticationProvider` or `Onboarding`), `Owner`, `Description`, `IsSecret` and `Location`. The requirements are not part of the executable semantic model and do not change its bytes.

## From the command line

```bash
screenplay environment                        # every .play file beneath the current directory
screenplay environment ./model --format json
```

The default output lists one key per line and marks secrets:

```text
authentication.providers.EntraId.clientId
authentication.providers.EntraId.clientSecret  (secret)
authentication.providers.EntraId.tenantId
```

`--format json` writes one document with `key`, `secret`, `source`, `owner` and `description` for every key. The command exits `0` when it listed the keys, `1` when the model has errors (it lists nothing for a model that does not compile) and `2` when it could not run. See [the tool](tool.md).
