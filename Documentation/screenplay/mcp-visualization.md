---
title: See a model as an event model board with MCP
description: Draw a Screenplay application as an event model board inside an MCP host, and see what a proposal or a what-if sketch would change before anything is written.
---

When the MCP host renders [MCP Apps](https://modelcontextprotocol.io/extensions/apps/overview)
views, the [Screenplay server](mcp.md) can draw the application as the same event
model board as Cratis Studio and the VS Code extension. Use it to look at what is
there, and to see what a change would do to the model before it is written.

The board is offered only to hosts that advertise the MCP Apps extension
(`io.modelcontextprotocol/ui`) when they connect. Other clients see the server's
usual tools, without `visualize-model`.

Use **Cratis.Screenplay.Tool 4.47.0 or later**, or **CLI versions that bundle
Screenplay 4.47.0 or later**. CLI hosting alone does not guarantee MCP Apps support.
See [installation](install-mcp.md) and [See your event model](see-your-event-model.md)
for the same board in other places.

![The MCP App shows an unsaved Notifications proposal beside Commerce fulfillment flows in a labeled capture harness](images/mcp-app-what-if-harness.png)

*The real MCP App in a local capture harness, not a commercial chat host. The
capture uses a single-file Commerce adaptation and an unsaved what-if sketch.*

## Show the model on disk

Ask for the board, or call `visualize-model` without arguments. The board shows
every `.play` document under the server root, compiled as one application. The
model receives a short summary - how many slices, events and errors the board
shows - and the documents themselves go only to the board.

The board has the Cratis logo in its upper left and its toolbar in its upper
right: *View* sets the detail level, whether properties are shown and whether
connections are drawn as arrows or lines. Where the host allows it, the toolbar
also draws the board again from disk and switches to fullscreen.

## See what a proposal would change

After an authoring call returns a `proposalId`, call `visualize-model` with it:

```json
{ "proposalId": "<proposalId>" }
```

The board starts on the application as the proposal would leave it. *Current* and
*Proposed* in the toolbar switch between that and the application as it is; what
both share keeps its place, so the difference is what moves. The summary lists
the modules, features, slices, commands, events, read models, screens and reactors
the proposal adds and removes. Nothing is written: review and `apply` the proposal
as described in [Create and edit a model with MCP](mcp-authoring.md#review-and-apply).

## Sketch a what-if

To see how a piece of functionality could look before shaping it into a proposal,
pass whole `.play` documents as a `sketch`. Screenplay 4.48.0 has known failures
with multi-file sketch paths; use a self-contained single-file model for this
workflow. For a model whose complete source is `application.play`, this scratch
example replaces that document for the view only:

```json
{
  "sketch": [
    { "path": "application.play", "source": "module Orders\n  feature Returns\n    slice StateChange RegisterReturn\n      command RegisterReturn\n        produces ReturnRegistered\n      event ReturnRegistered\n" }
  ]
}
```

Each document replaces the one at its path, or is added when there is none. The
sketch is drawn over the documents on disk exactly as a proposal is, with the same
*Current* and *Proposed* switch and the same summary. It is never validated as a
proposal, never kept, and never written; a sketch that does not compile is drawn
as far as it can be read, with the number of errors shown on the board. Pass
either `proposalId` or `sketch`, not both.

Ask for the board again after each change to the sketch or the model: every call
draws a new board, from the model as it is at that moment.
