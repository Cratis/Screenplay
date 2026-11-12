---
title: "Screenplay environment bindings"
description: "Find the configuration keys derived from Screenplay authentication and onboarding declarations, and supply their values through your application host."
---

# Environments

Screenplay environment bindings name the configuration values an application host must supply for declared sign-in providers and onboarding. They let you keep the same model in test and production while using different client registrations, mail servers and credentials.

Declare [authentication providers](authentication.md) first and [onboarding](onboarding.md) if needed, then list the derived keys below. The host owns value lookup and integration with those services. Screenplay lists requirements; it does not supply credentials or verify a connection to the provider or mail server.

## What belongs where

| In the model | In the environment |
|---|---|
| Which identity providers the application signs people in with (`authentication`) | Each provider's client id, client secret and authority |
| That onboarding sends emails, and which templates it uses | The sender address, the mail server and its credentials |
| Personas, policies, events, screens | These declarations add no environment keys |

Keep environment-specific values out of the model. Configuration lines under an authentication provider are a compile error. Environment requirements carry only a secret's key, never its value; the host chooses where to store and retrieve the value.

## The binding convention

Screenplay derives each key from a declaration. Use these dotted, case-sensitive paths when connecting the requirements to host configuration. A provider alias is part of its keys, so renaming it also changes the keys the host must supply.

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

This declaration needs `authentication.providers.EntraId.clientId`, `authentication.providers.EntraId.clientSecret` and `authentication.providers.EntraId.tenantId`. Each of `authentication.providers.Partner` and `authentication.providers.Supplier` needs `clientId`, `clientSecret` and `authority`. The aliases keep the two `OpenId` registrations distinct; without them, both would resolve to `OpenId` and cause a compile error (`PLAY0169`).

### Onboarding

Any [`onboarding`](onboarding.md) block adds these mail settings to the requirements, even if it does not declare email templates:

| Key | Secret |
|---|---|
| `onboarding.email.sender` | no |
| `onboarding.email.server.host` | no |
| `onboarding.email.server.port` | no |
| `onboarding.email.server.username` | no |
| `onboarding.email.server.password` | **yes** |

## Listing the keys a model needs

A host can read the requirements from a successful compilation and check for missing values before sign-in or email delivery. That check must be implemented by the host; a successful compilation does not mean the environment is configured.

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

## System settings and named secrets

A `system` realized by an extension declares settings in its contract. The model says where each value comes from:

```play
secret MailerApiKey
  description "The API key of the transactional mail service"

system Mailer
  extension "ghcr.io/cratis/extension-mail" version 2
  setting apiKey from secret MailerApiKey
  setting senderName = "Invoicing"
  activation on demand idle after 10 minutes
```

- **Implicit.** A setting the model does not mention is read from configuration under `systems.<System>.<setting>`, following the binding convention above.
- **Named secret.** `secret <Name>` is declared once at the top level, with an optional `description`. `setting <name> from secret <Name>` maps a setting to it, so one secret can serve several systems. The deployment binds the secret name to its store; the model never holds a value.
- **Literal.** `setting <name> = <value>` is for non-secret settings only, and the environment may override it. A literal on a secret setting is an error.
- **Activation.** `activation always` or `activation on demand [idle after <n> <unit>]` overrides the contract's default.

An unknown setting, an unknown or unused secret and a duplicate mapping are errors; see [diagnostics](diagnostics.md) `PLAY0990` to `PLAY0997`. The `extension`, `setting` and `activation` lines are realization metadata: they change no bytes of the executable model, are exposed on the semantic compilation (`SystemConfigurations` and `Secrets`), and renderers include them in their render input.

`EnvironmentRequirements` lists the implicit keys and the named secrets, each once, with its secret flag. The requirements inventory is implemented in C#; the TypeScript compiler parses and validates the same forms and exposes them on its syntax tree, but does not derive environment requirements.

See also [Systems and abilities](systems.md). The CLI configuration documentation (Cratis/cli) links back here for the `systems.<System>.<setting>` keys.

## From the command line

```bash
screenplay environment                        # every .play file beneath the current directory
screenplay environment ./model --format json
```

The default output lists one key per line and marks secrets. This is a requirements inventory, not a check of the values installed in an environment:

```text
authentication.providers.EntraId.clientId
authentication.providers.EntraId.clientSecret  (secret)
authentication.providers.EntraId.tenantId
```

`--format json` writes one document with `key`, `secret`, `source`, `owner` and `description` for every key. The command exits `0` when it listed the keys, `1` when the model has errors (it lists nothing for a model that does not compile) and `2` when it could not run. See [the tool](tool.md).
