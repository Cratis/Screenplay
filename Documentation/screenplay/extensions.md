---
title: "Screenplay extensions: systems, abilities and extension packages"
description: "How a Cratis Screenplay model reaches the outside world: systems and abilities, the single invokes verb, durable invocations, extension packages, the lock, activation and execution tiers, with a map to the owning pages in every repository."
---

# Extensions

A model often has to reach something it does not own: a carrier that books a pickup, a payment provider, a chat tool where someone approves a request. Screenplay keeps that reach declarative. The model says what the business can ask and what can come back. An *extension* is the code that does it, and it is chosen outside the model.

This page explains how the pieces fit and points to the page that owns each one. Start here when you think "I need Screenplay to talk to X" and follow the links.

## The vocabulary

| Term | Meaning |
| --- | --- |
| System | A named external system the business can ask for something. See [Systems and abilities](systems.md). |
| Ability | One typed thing a system can be asked to do: inputs, optional returned values, named outcomes and an effect class. |
| Outcome | A named business result other than success, such as a refusal the external system gives. |
| Effect class | `safe`, `idempotent` or `once`: what a retry may do when an attempt's result is unknown. |
| Extension | The realization of a system: the code and assets that carry its abilities out. |
| Extension package | The distributable unit of an extension. Its `contract.play` declares what it contributes. See [Extension contracts](extension-contracts.md). |
| Invocation | One durable request to perform an ability, with an identity of its own. |
| Activation | When an extension's backend starts and stops. |
| Placeholder | The static HTML that design tools draw in place of an extension's component. |
| Lock | `screenplay.lock`, which pins every extension reference to an exact release. |

The [glossary](glossary.md) holds one line for each.

## One verb: `invokes`

A reaction, a workflow action and a state's `entering` line all ask a system for an ability with `invokes <System>.<Ability>`. The dotted name tells an ability from a command. Each input is mapped once, and the answer is handled where the call is made, with `on returned`, one branch per declared outcome and `on failed`. See [Invoke an ability](systems.md#invoke-an-ability), and [reactions](reactions.md), [workflows](workflows.md) and [state machines](statemachines.md) for the places it appears.

`invokes` replaced the older `sends` in workflows, state machines and specifications. Operation phases that invoke an ability are covered in [Operations and external systems](operations.md). A command written in C# invokes abilities through [Arc's operations](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/commands/operations/abilities.md).

An invocation's result is never turned into an event implicitly. The branch says which event records which outcome.

## Code computes, abilities touch the world

Inline code that only computes may run anywhere and any number of times. Anything with an external effect goes through an ability, so it gets an identity, an idempotency key, retries, a recorded outcome and a place in specifications. This is the reason the effect class is part of the declaration, and `once` is the default: it is the only class that is never wrong. See [Choose the effect class](systems.md#choose-the-effect-class).

## How an invocation runs

Stage runs every ability invocation as a persisted message through Choreography, on the Orleans silo it already hosts. In short:

1. The invocation is recorded with its cause before any work starts, so it is never held only in memory.
1. One run handles the invocation, keyed by its identity, so asking twice does not call the system twice. The run pins the locked package digest, so upgrading an extension never changes an invocation in flight.
1. Deployment limits on concurrent calls apply, then the extension is called.
1. The outcome is recorded and handed back to the behavior that asked.

`once` abilities are never retried after an attempt that may have reached the system. They stop for reconciliation. An ability that answers later suspends its run until the answer arrives. The owning pages are Stage's [Durable ability invocations](https://github.com/Cratis/Stage/blob/main/Documentation/reference/ability-invocations.md) and Choreography's [answers that arrive later, capability gates and reconciliation](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/extension-invocations.md).

Specifications script an ability's answer without running anything. See [Ability answers](specifications.md#ability-answers).

## Packages, references, the lock and activation

A system that names a package declares no abilities of its own. The package's `contract.play` provides them, so the contract and the model cannot drift.

```screenplay
system Payments
  extension "cratis/stripe" version 2
  setting secretKey from secret StripeSecretKey
  activation on demand idle after 10 minutes
```

- **References.** A reference is a catalog short name, a full OCI reference or an `npm:` package for a frontend-only package. Without a version it means the latest release. See [Referencing a package](extension-contracts.md#referencing-a-package).
- **The lock.** `screenplay.lock` pins each reference to a version, a package digest, a contract hash and a publisher. Commit it. The compiler never resolves a range: it reads the contract by the locked digest from the vendored `extensions` folder or the user cache. See [The lock](extension-contracts.md#the-lock) and [the cache, vendoring and restore](extension-contracts.md#the-cache-vendoring-and-restore).
- **Naming is not trusting.** Whether a package may run is decided by an allow-list of publishers in `.cratis/extensions.json` and, for a deployment, by Stage. See the CLI's [trust rules](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md#trust) and Stage's [Extension packages](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md).
- **Settings and secrets.** A system maps each setting its contract declares to a literal or a named secret, never a secret's value. See [System settings and named secrets](environments.md#system-settings-and-named-secrets).
- **Activation and tiers.** A backend runs in an execution tier and starts lazily. Reaching a screen that places the system's components starts the backend. Mounting a component does not. See Stage's [Execution tiers and activation](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/execution-tiers.md).

The `extension`, `setting` and `activation` lines are realization metadata. They change no bytes of the executable semantic model, so swapping one provider for another does not change what an invocation means.

## What a package can contribute

A contract can declare abilities, inbound triggers, settings, a backend and frontend components. A screen places a component with `component <System>.<Component> <name>`, names the system and never the package, and handles the signals the component raises. See [Extension components](screens.md#extension-components) and [Interaction triggers](interactions.md#interaction-triggers). Inbound traffic such as a webhook is stored and acknowledged by Stage and delivered to the extension, which raises the contract's triggers; see Stage's [inbound traffic](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/inbound-traffic.md) and [triggers](triggers.md).

An ability can also be realized without writing a backend, by the built-in [`http` extension](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/http.md), which maps an ability to a REST call from a declarative block.

## Where each part is documented

| You want to | Read |
| --- | --- |
| Declare a system and its abilities | [Systems and abilities](systems.md) |
| Invoke an ability from a behavior | [Invoke an ability](systems.md#invoke-an-ability) |
| Write a specification for an invocation | [Ability answers](specifications.md#ability-answers) |
| Understand the contract file, the manifest and the lock | [Extension contracts](extension-contracts.md) |
| Configure settings, secrets and activation | [Environments](environments.md#system-settings-and-named-secrets) |
| Place an extension component on a screen | [Extension components](screens.md#extension-components) |
| Restore, update and vendor packages | [`cratis extension`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md) and [Restore extensions](tool.md#restore-extensions) |
| Run a model that uses extensions | [`cratis run`](https://github.com/Cratis/cli/blob/main/Documentation/reference/run.md#extensions) |
| Create and verify a package | [`cratis extension new`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-new.md), [`cratis extension verify`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-verify.md) and the [extension templates](https://github.com/Cratis/Templates/blob/main/Documentation/extensions/index.mdx) |
| Write a backend | Stage's [extension backends](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/index.md): [protocol](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/protocol.md), [host services](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/host-services.md) and SDKs for [C#](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/csharp.md), [Kotlin](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/kotlin.md), [Go](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/go.md) and [TypeScript](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/typescript.md) |
| Write a frontend component | Scene's [extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md) and [Build an extension component](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/build-a-component.md) |
| Invoke abilities from a C# command | [Arc operations that invoke abilities](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/commands/operations/abilities.md) |
| Understand why it is designed this way | [Decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md) and [decision 0160](https://github.com/Cratis/Screenplay/blob/main/decisions/0160-extension-contract-file-the-lock-and-restore.md) |

## Next

- [Using an extension](using-an-extension.md) adds a package to a model and runs it.
- [Building an extension](building-an-extension.md) creates, verifies and publishes one.
