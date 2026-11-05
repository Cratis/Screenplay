---
title: "Screenplay triggers: name signals that start reactions"
description: "Declare the name and payload of an application signal in Screenplay so reactions can consume it. The host or integration still produces the signal."
---

# Triggers

An application trigger in Cratis Screenplay names a signal and the values it carries so a [reaction](reactions.md) can respond to it. Declare one when an integration or host raises something that is not already a domain event. Domain events and clock schedules can trigger reactions without an application-trigger declaration.

For an order-fulfilment process, a carrier integration might raise a signal when it receives a delivery update. The trigger describes that signal's shape; the integration remains responsible for producing it. If delivery is already recorded as a declared event, a reaction can use that event directly.

Application triggers differ from UI interaction triggers. A UI behavior's `on click` or `on enter` clause needs no declaration. An interaction can also observe an application trigger with `on <ApplicationTrigger>` or fire one with `raise <ApplicationTrigger>`; see [Interactions](interactions.md). Do not give a declared trigger the name of a built-in interaction kind: that is an error because `on <Name>` would mean the UI interaction.

## Syntax

```screenplay
trigger <Name>
  [description "<text>"]
  [file <path>]
  [<name> [<Type>]]*
```

Use the optional `file` line to point to the repository-relative implementation file. It supports navigation back to code, but does not replace any part of the declaration or arrange delivery of the signal. See [File references](file-references.md).

A trigger body reserves `file`, so a trigger value by that name is written `@file`.

Declare the trigger at the top level, beside `concept`, `type` and `policy`, so it can be used across slices.

## What a trigger declares

A trigger declares two things: that the name exists, and what an occurrence of it hands the reaction.

```screenplay
trigger GitHubIssueCreated
  description "GitHub reported a new issue on a watched repository"
  repository Repository
  issue Issue
```

Here, each occurrence supplies `repository` and `issue`. The declaration does not connect to GitHub or subscribe to issue updates. The producer owns that work; the compiler uses the name and payload shape to resolve a reaction's `when` and report selections of values the trigger does not carry.

The type on a value is optional:

```screenplay
trigger DirectoryChanged
  entry
  changedAt DateTime
```

A bare value name is valid while its type is undecided. In this example, the occurrence supplies `entry` with an unspecified type and `changedAt` as a `DateTime`. Add a type when the model can state one.

## Using one

A reaction names a trigger with `when`, exactly as it names an event:

```screenplay
trigger BuildFinished
  repository
  outcome

module Delivery
  feature Builds
    slice Automation NotifyOnFailure
      reaction NotifyOnFailure
        when BuildFinished
          repository
          outcome
        where outcome == "failed"
```

This reaction selects `repository` and `outcome`, then filters for failed builds. It declares no effects yet. Continue with [Trigger → reaction → effects](reactions.md#trigger--reaction--effects) to add an event production or command invocation. Declaring the trigger does not give the reference runner a provider for that signal.

## How a name resolves

`when <Name>` is resolved against three sets, in the order they sit closest to the document:

| Order | Set | Declared by |
| --- | --- | --- |
| 1 | events the document declares or imports | `event <Name>` / `import <Qualified.Name>` |
| 2 | triggers the document declares | `trigger <Name>` |
| 3 | triggers registered with the compiler, plus the built-in host signals | a consumer, or the language |

If an event and a declared trigger share a name, the event wins. Both reaction value validation and
executable binding use the event's shape, including values used by `produces ... for <value>`; the shadowed
trigger's shape does not contribute. An imported event also wins, but its unknown shape cannot be checked
against the declared trigger's values. Rename and reference queries follow that same precedence: renaming
an event updates its reaction's `when` reference, while editing a shadowed trigger's name leaves it alone.
Editor hover and go-to-definition select the event too; an external import has no local definition to open.

A registration wins over a built-in of the same name, so a host that raises a richer `Startup` can say what
it carries rather than being overruled by the empty one the language ships.

Only when all three miss is the name reported as unknown — as a warning, like every other unresolved
reference in the language.

## Built-in triggers

| Trigger | Occurs |
| --- | --- |
| `Startup` | when the application starts |
| `Shutdown` | when the application stops |

These need no declaration because every application has them and there is no event to declare for them.
The clock is built in too, but with its own syntax rather than a name — see
[what sets a reaction off](reactions.md#what-sets-a-reaction-off). Clock triggers (`every` and `at`) take no values, so value lines beneath them are errors. A scalar `reads` line takes no indented children either; composite read keys use a named `by` block. See [reaction reads](reactions.md#views-a-reaction-decides-from).

## Registering a trigger

A trigger an integration provides has no declaration in the document that reacts to it — the integration is
what knows it exists. A consumer tells the compiler about it through the same registry that carries the
[inline sub-languages](sub-languages.md):

```csharp
var compiler = new ScreenplayCompiler(
    new ScreenplayLanguageRegistry(triggers: ["GitPushed", "PullRequestMerged"]));
```

That form says only that the compiler recognizes the name. A registration can also say what an occurrence
hands the reaction, and then a reaction taking anything else is reported exactly as it is for a declared
trigger:

```csharp
var compiler = new ScreenplayCompiler(
    new ScreenplayLanguageRegistry(triggers:
    [
        new TriggerDefinition("GitPushed", ["repository", "branch", "sha"]),
        new TriggerDefinition("PullRequestMerged", ["repository", "number"])
    ]));
```

Omitting `Values` and supplying an empty list mean different things. A `TriggerDefinition` with no `Values` leaves the shape unspecified, so the compiler leaves a reaction's value selections unchecked. An empty list says an occurrence carries nothing, so selecting any value is reported. The built-in `Startup` and `Shutdown` use an empty list.

Registration gives the compiler a name and optional shape, not a runtime producer. Use a `trigger` declaration instead when the application document needs to explain the signal's meaning or payload to its readers.
