---
title: Screenplay
description: "Choose the optional model-first path: write business behavior in Screenplay, check reference specifications, and select compatible consumers. Arc and Chronicle remain independently usable."
---

# Documentation

Screenplay is the Cratis Platform's optional model-first language. A `.play` model
keeps commands, facts, rules, views, and specifications together so you can review
what the system should do before choosing its implementation.

You can use [Arc](/arc/) and [Chronicle](/chronicle/) directly without adopting
Screenplay, Stage, Studio, Scene, or Prologue. A model is another development path,
not a mandatory layer over those components.

## Start with a result

- [Write and test a first model](screenplay/getting-started.md): copy one complete
  library file, run three supported reference scenarios, deliberately break an
  expectation, and restore the passing result. Only the released .NET tool is needed.
- [Decide whether it fits](screenplay/why-screenplay.md): weigh an explicit shared
  model against keeping handwritten slices.
- [See an event model](screenplay/see-your-event-model.md): inspect authored source
  or generate a starting point from existing code. A board is not an execution verdict.

## Reference and next steps

[Screenplay Language](screenplay/index.mdx) connects the language overview,
construct reference, glossary, and full grammar. For automation, use
[Compiler and CLI](screenplay/tool.md); for a runtime or renderer, use
[Interoperability](screenplay/interoperability.md).

Source validation, executable binding, reference evaluation, and consumer
conformance are separate checks. A passing in-memory specification does not deploy
an application, commit events to Chronicle, or execute attached code.
