# Triggers

A trigger is something that can cause a [reaction](reactions.md) to run. Domain events and the clock are
built in; everything else is declared, so the set of things an application can react to is open without the
language having to grow a keyword per source.

## Syntax

```screenplay
trigger <Name>
  [description "<text>"]
  [<name> [<Type>]]*
```

A trigger is declared at the top level of a document, beside `concept`, `type` and `policy`, because what
can happen to an application is not the property of one slice.

## What a trigger declares

A trigger declares two things: that the name exists, and what an occurrence of it hands the reaction.

```screenplay
trigger GitHubIssueCreated
  description "GitHub reported a new issue on a watched repository"
  repository Repository
  issue Issue
```

It deliberately declares nothing about *what makes one occur*. That belongs to whatever produces it — an
integration, a runtime signal, a person — and keeping it out of the document is what lets the set be open at
all. The compiler's whole job here is to know the name and the shape, so a reaction naming it resolves and a
reaction taking a value it does not carry is reported.

The type on a value is optional:

```screenplay
trigger DirectoryChanged
  entry
  changedAt DateTime
```

A trigger is often declared before the shape of what it carries has settled, and a bare name is already a
useful statement — the reaction is handed something by that name. Add the type when there is one to state.

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

That sameness is the point. A reaction should not have to know whether what set it off came from the event
store, a clock, an integration or a custom runtime provider — see
[Trigger → reaction → effects](reactions.md#trigger--reaction--effects).

## How a name resolves

`when <Name>` is resolved against three sets, in the order they sit closest to the document:

| Order | Set | Declared by |
| --- | --- | --- |
| 1 | events the document declares or imports | `event <Name>` / `import <Qualified.Name>` |
| 2 | triggers the document declares | `trigger <Name>` |
| 3 | triggers registered with the compiler, plus the built-in host signals | a consumer, or the language |

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
[what sets a reaction off](reactions.md#what-sets-a-reaction-off).

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

**Stating no values and stating none are different claims.** A `TriggerDefinition` with no `Values` says the
registration does not describe the shape, so what a reaction takes is left alone. One with an empty list says
an occurrence carries nothing, so taking something from it is reported. The built-in `Startup` and `Shutdown`
are registered the second way.

What a registration never says is what makes an occurrence happen. That boundary is the same one the inline
languages draw, and it is what keeps the set open.

A trigger the document itself needs to describe — its values, or what it means — is better written as a
`trigger` declaration, where a reader can see it.
