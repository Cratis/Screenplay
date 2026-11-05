---
title: Cratis Screenplay documentation
description: Cratis Screenplay is a declarative .play language for modeling business behavior through commands, events, read models, screens, and specifications.
---

Cratis Screenplay is a declarative modeling language that describes events, commands, read models (queryable views of recorded facts), screens, and specifications in one portable `.play` model. People and AI agents can read the same description of a feature rather than piece its behavior together from separate implementation layers.

Start with [Getting started](screenplay/getting-started.md) to model borrowing a library book and try editor highlighting, completions, and diagnostics. The tutorial ends with a model you can review; it does not deploy or run an application.

When you choose a tool to execute or render a model, check which capabilities it supports. The compiler's executable semantic model (ESM) records versioned behavior that each consumer must preserve. Where Cratis Chronicle supplies runtime behavior, the ESM follows that meaning rather than letting each tool invent its own.

To try a supported model through an API or its screens, follow the [Cratis Stage sandbox walkthrough](/stage/docker/); [Stage's documentation](/stage/) explains its execution scope. It is not a production host: state is held in memory and lost on container restart. Check admission diagnostics and bind published ports to `127.0.0.1`.

## Contents

| If you want to… | Read |
| --- | --- |
| Choose a learning path or explore model consumers | [Screenplay language](screenplay/index.mdx) |
| Find design principles, file structure, and language reference links | [Language overview](screenplay/overview.md) |
| Decide what to model and what to keep hand-written | [Why Screenplay](screenplay/why-screenplay.mdx) |
