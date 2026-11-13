---
title: "Use an extension in a Screenplay model"
description: "Add a system that references an extension package, restore it into the lock, configure settings and secrets, invoke abilities, place components on screens, specify the result, run it with cratis run and update it later."
---

# Use an extension

This guide adds an extension package to a model, uses what it provides and keeps it current. For what the pieces are, read the [extensions overview](extensions.md) first.

The examples use a payment package. Replace the name with the package you use.

## 1. Add a system that references the package

A `system` names the business system. Its `extension` line names the package:

```screenplay
secret StripeSecretKey
  description "The key the payment provider issues"

system Payments
  description "The card payment provider"
  extension "cratis/stripe" version 2
  setting secretKey from secret StripeSecretKey
  setting statementDescriptor = "Acme Shop"
  activation on demand idle after 10 minutes
```

- With no `version`, the reference means the latest release. `version 2` means the latest 2.x, `version 2.3.1` exactly that release. All forms are listed in [Referencing a package](extension-contracts.md#referencing-a-package).
- A system that names an extension declares no abilities of its own. Declaring one is `PLAY1106`.
- The reference is a catalog short name (`"cratis/stripe"`), a full OCI reference (`"ghcr.io/acme/extension-ledger"`) or an `npm:` package for a frontend-only package.

## 2. Restore it

The compiler never fetches a package. It reads each contract from `screenplay.lock` and a digest-keyed cache, so pin and restore first:

```bash
cratis extension restore
```

A reference the lock does not have yet is resolved to the newest release in its range, checked, stored in the cache and written to `screenplay.lock`. Commit the lock. Until you restore, the compiler reports `PLAY1107` (not in the lock) or `PLAY1108` (not restored). The language server and MCP server only warn and restore for you when `SCREENPLAY_EXTENSION_SOURCE` names a package source.

In CI, use `cratis extension restore --locked`. It never changes the lock and fails for a reference the lock does not pin or pins for another range. For a model folder without network access, `screenplay restore` reads packages from a directory; see [Restore extensions](tool.md#restore-extensions). The CLI reference is [`cratis extension restore`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-restore.md).

Naming a package is not trusting it. Restore refuses a package whose publisher is not on the allow-list in `.cratis/extensions.json`; in an interactive terminal it asks once. The rules are in the CLI's [trust section](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md#trust).

## 3. Configure settings and secrets

Each setting the contract declares is mapped in the model to a literal (for settings that are not secret) or to a named secret. A setting the model does not mention is read from configuration under `systems.<System>.<setting>`. The model never holds a secret's value. See [System settings and named secrets](environments.md#system-settings-and-named-secrets).

Give the values to the environment, then list what is missing:

```bash
cratis config --requires ./model
cratis config set secrets.StripeSecretKey
```

`cratis config set` reads the secret from standard input or a prompt. The CLI documents environments in [Environments](https://github.com/Cratis/cli/blob/main/Documentation/environments/index.md#extension-settings-and-secrets).

## 4. Invoke abilities

The abilities of the package are now abilities of `Payments`. Invoke one from a reaction, a workflow action or a state's `entering` line, and handle each way it can end:

```play
reaction TakePayment
  runs as system
  when OrderPlaced
    orderId
    total
    invokes Payments.CreatePaymentIntent
      amount = total
      orderId = orderId
      on returned
        produces PaymentRequested
          clientSecret = $result.clientSecret
      on failed
        produces PaymentFailed
          reason = $failure.reason
```

Every input the ability declares is mapped once, and `on <Outcome>` handles an outcome the contract declares. The rules and diagnostics are in [Invoke an ability](systems.md#invoke-an-ability). The effect class of the ability decides retries: a `once` ability is never retried after an attempt that may have reached the provider. See [Choose the effect class](systems.md#choose-the-effect-class).

A contract can also declare triggers. They become triggers of the system, so a reaction can start with `when Payments.PaymentSucceeded`.

## 5. Place components on screens

A screen places a component by naming the system and the component, never the package, and handles the signals the contract says it raises:

```play
component Payments.PaymentForm payment
  property amount = "10"
  on succeeded
    execute ConfirmPayment
      with paymentIntentId from signal.paymentIntentId
  on failed
    notify warning "The payment failed"
```

The component, its properties, the signals handled with `on` and the values read as `signal.<value>` are checked against the contract. See [Extension components](screens.md#extension-components).

## 6. Specify it

A reaction specification scripts what an ability answers while the reaction runs, so nothing external is called. The lines are `given <System>.<Ability>` followed by `returns`, `answers <Outcome>` or `fails "<reason>"`:

```play
given Carrier.BookPickup returns
  trackingNumber = "TRK-1"
given Carrier.BookPickup answers NoCapacity
given Carrier.BookPickup fails "carrier unavailable"
```

Each line is checked against the system's declared ability, outcomes and returned fields (`PLAY1017`). `then invokes <System>.<Ability>` asserts the call. See [Ability answers](specifications.md#ability-answers).

## 7. Run it

```bash
cratis run ./model -e development
```

Before Docker starts, `cratis run` checks that every `extension` line is pinned in the lock, that the publisher is allowed, that the contracts are in the cache and that the environment holds every setting and secret. Nothing starts when one is refused. Stage then fetches each package itself from the lock, verifies it again, and starts each backend in its tier when it is first needed. See [`cratis run`](https://github.com/Cratis/cli/blob/main/Documentation/reference/run.md#extensions) and Stage's [Extension packages](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md).

### Host-provided ability answers

A .NET live host can pass an `ISemanticAbilityInvoker` to `SemanticReactionSettlement` and call `SettleAsync`. The invoker receives the system, ability, evaluated inputs, reaction, causing fact, occurrence time and a stable invocation identity. It returns a `SemanticAbilityAnswer`: `Returned` selects `on returned` and supplies `$result.<field>`, `Outcome` selects the named branch and supplies `$outcome`, and `Failed` selects `on failed` and supplies `$reason`. A missing branch produces no facts. Host exceptions and cancellation propagate rather than becoming authored failure answers.

```csharp
var settlement = new SemanticReactionSettlement(evaluator, abilityInvoker);
var result = await settlement.SettleAsync(plan, world, appended, occurred, cancellationToken);
```

The world must contain `appended` as its last facts and the complete log prefix. The identity combines the reaction, the causing fact's zero-based position in that log and the raw trigger and invocation ordinals, counting every trigger and invocation regardless of filtering. A host uses `context.Identity.Key` to deduplicate external effects across retries. Equal facts at different positions have different identities; a trimmed world is not a substitute for the complete prefix.

An invoker may return `SemanticAbilityAnswerKind.Pending` when it cannot answer yet. No answer branch runs. An accepted result exposes `PendingInvocations`; the host persists these data records alongside the accepted batch, then later supplies a completed answer:

```csharp
var resumed = await settlement.SettleAnswerAsync(plan, currentWorld, pending, answer, cancellationToken);
```

`SettleAnswerAsync` uses the pending record's raw `TriggerIndex` and `InvocationIndex` to restore the saved cause and occurrence, run only the selected answer branch, and settle its new facts and cascades. It never re-evaluates the trigger's `where` condition. It does not call the pending ability again. The host must deduplicate completed answers by identity: resuming the same record twice can append duplicate facts. The host also owns transport, timeouts and delivery retries. Rejected settlement leaves the input world unchanged, but cannot undo an external effect already performed by the invoker.

Without an invoker, settlement keeps the deterministic specification behavior: authored `given` answers select branches, or an absent answer selects the default returned branch. `Pending` is host-only, not an authored `given` answer. The existing synchronous `Settle` remains available without an invoker. It throws `InvalidOperationException` when an invoker is configured: use `SettleAsync` instead. Body reactions and non-event triggers remain on the existing specification path; this hook covers live event-triggered settlement only.

## 8. Update it later

```bash
cratis extension update --dry-run
cratis extension update
```

`update` moves each reference to the newest release in its range and shows how the contract changes: what it adds, what it no longer offers and every use in your model that the new contract does not offer. When a use would break, the lock is left alone unless you pass `--allow-incompatible`. To update one extension, name its system: `cratis extension update Payments`. See [`cratis extension update`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-update.md) and [Provider compatibility](extension-contracts.md#provider-compatibility).

To work offline or from a private mirror, copy the locked packages with [`cratis extension vendor`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-vendor.md); the vendored `extensions` folder beside the lock is read before the user cache.

## Next

- [Building an extension](building-an-extension.md) creates a package of your own.
- [Systems and abilities](systems.md) and [Extension contracts](extension-contracts.md) are the references.
