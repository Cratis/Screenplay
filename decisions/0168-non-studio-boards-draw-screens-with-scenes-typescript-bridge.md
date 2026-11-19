---
id: 0168
title: The VS Code board and the MCP App draw a .play screen with Scene's TypeScript bridge, bundled eagerly
status: proposed
stage: implemented
class: contract
reversibility: costly
decided: Pending - records behavior implemented for Cratis/StudioIssues#1420; a maintainer accepts or amends it
decider: Pending
applies-to:
  - Source/Screenplay/EventModels/Screens/**
  - Source/Screenplay/Views/Screens/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/McpApp/**
  - Documentation/screenplay/vscode.md
  - Documentation/screenplay/mcp/view.md
---

## Context

The event model board holds nothing of a screen's design. It knows only that a role has one, and a host that
can draw the screen hands it the Scene document (`Cratis/Studio` decision 0026). Studio Core was the only host
that could, so every screen on the VS Code board and the MCP App was an empty window
([Cratis/StudioIssues#1420](https://github.com/Cratis/StudioIssues/issues/1420)).

Turning a `.play` screen into a Scene document existed only in C#, in `Cratis.Scene.Screenplay`. Neither of
these hosts has C# at the moment it draws: the webview is a browser page in the editor, and the MCP App is one
HTML page in a host's sandbox. The extension's only C# bridge is an opt-in server the user installs, which is
not present when a board is opened.

## Decision

- **The conversion is Scene's, not ours.** `@cratis/scene.screenplay` is Scene's own TypeScript mirror of that
  bridge, held to the C# one by a shared generated corpus. This repository never re-implements what a `.play`
  screen means as a Scene document, and never keeps a second copy of the conversion in a host.
- **The documents travel beside the board document, never in it.** `screensOfApplication` walks the
  application the way the board document's own mapping walks it and keys each document by the slice and role
  the board asks for. The board document keeps saying only `hasScreen`; the board itself stays Scene-free.
- **Both hosts draw, render-only.** The screen is drawn through the board's `screens` hook, which a read-only
  board offers. Nothing in a screen can be pressed or typed into, and no host outside Studio opens or edits a
  screen.
- **A screen that cannot be drawn is a note, never an error.** A screen whose design lives in a file - which
  only the Studio backend reads - keeps the board's own placeholder window, and the host says once which
  screen it was, in the surface's information colours. The board is drawn as usual. A `.play` module's `form`
  is not part of the model the TypeScript compiler produces, so a form's action is drawn as its button.
- **The theme is the board's.** A screen is drawn in the design system's own dark or light theme, matching the
  surface the board is on, so it does not appear as a light sheet on a dark board. No host names a colour.
- **Scene's renderer is bundled eagerly, under a stated budget.** The MCP App's sandbox loads nothing from the
  server, so its page has no second chunk to fetch; bundling the renderer on demand is not available there,
  and the VS Code webview is bundled as one IIFE for its content security policy. Both builds therefore ship
  the renderer in full and refuse to grow past a budget recorded in the build script.

## Consequences

- The shipped webview bundle grew from 2.16 MB to 7.08 MB and the MCP App's page from 3.19 MB to 8.34 MB:
  Scene's renderer, its blueprint packages and PrimeReact. The webview is read from disk inside the editor;
  the MCP App's page is downloaded by the host, which is the heavier of the two and the reason its budget is
  stated separately. Lowering either needs code splitting the MCP sandbox does not allow today.
- This repository now depends on released `@cratis/scene.screenplay`, `@cratis/scene.editor` and
  `@cratis/event-models`, so a change to what a `.play` screen means reaches the board only through a Scene
  release. That is the intended direction: Scene owns the meaning.
- Open and not decided here: whether the MCP App should keep the placeholder window instead of paying for the
  renderer in its page, and whether `form` should enter the TypeScript model so a screen's forms draw as forms.
