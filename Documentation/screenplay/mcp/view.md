---
title: View a model
description: Draw a Screenplay application as an event model board inside an MCP host, and see what a proposal or a what-if sketch would change before anything is written.
---

When the MCP host renders [MCP Apps](https://modelcontextprotocol.io/extensions/apps/overview)
views, the [Screenplay server](reference.md) can draw the application as the same event
model board as Cratis Studio and the VS Code extension. Use it to look at what is
there, and to see what a change would do to the model before it is written.

## Which clients show the board

The board is an MCP App: an HTML view the host draws inside the conversation. The
server offers it, as the `visualize-model` tool, only to hosts that advertise the MCP
Apps extension (`io.modelcontextprotocol/ui`) when they connect. Other clients see
the server's usual tools, without `visualize-model`.

| Client | Draws the board |
| --- | --- |
| Claude Desktop | Yes |
| GitHub Copilot in VS Code | Yes |
| Claude Code | No, it is a terminal client |
| Codex | Not listed by the MCP Apps documentation as a host that draws views |

The [MCP Apps documentation](https://modelcontextprotocol.io/extensions/apps/overview)
keeps the current list of hosts. In a client that does not draw the board, use the
[VS Code extension](../vscode.md) to see a model, and keep using
[Explore a model](explore.md) in the terminal.

## Show the model on disk

```text
Show me the application as an event model board.
```

The assistant calls `visualize-model` without arguments. The board shows every
`.play` document under the server root, compiled as one application. The model
receives a short summary, such as how many slices, events and errors the board
shows. The documents themselves go only to the board.

The board has the Cratis logo in its upper left and its toolbar in its upper
right. *View* sets the detail level, whether properties are shown and whether
connections are drawn as arrows or lines. Where the host allows it, the toolbar
also draws the board again from disk and switches to fullscreen.

The board follows the model on disk. While it is on screen it checks every couple of
seconds and draws again when a file changed, whether the assistant applied a change
or you edited a file by hand. Open it fullscreen and keep prompting: each applied
change shows up on its own. A proposal you are looking at stays until the files
change, and then the board shows the application as it is. The refresh button in the
toolbar draws it again at once, and reports why when it cannot.

Hosts that do not let a view call the server cannot follow the files. Ask for the
board again there; every call draws the model as it is at that moment.

## See what a proposal would change

Ask for a change, and ask to see it before it is applied:

```text
Add a ReturnBook slice to the Lending module that produces BookReturned. Propose
it, then show me the board for that proposal so I can see what it adds.
```

After an authoring call returns a `proposalId`, the assistant calls `visualize-model`
with it:

```json
{ "proposalId": "<proposalId>" }
```

The board starts on the application as the proposal would leave it. *Current* and
*Proposed* in the toolbar switch between that and the application as it is. What
both share keeps its place, so the difference is what moves. The summary lists
the modules, features, slices, commands, events, read models, screens and reactors
the proposal adds and removes. Nothing is written: review and `apply` the proposal
as described in [Edit a model](edit.md#review-and-apply).

## Sketch a what-if

To see how a piece of functionality could look before shaping it into a proposal,
ask for a sketch:

```text
Without changing anything, sketch what a Reservations feature could look like next
to the existing Lending module, and show it on the board. Do not propose it yet.
```

The assistant passes whole `.play` documents as a `sketch`:

```json
{
  "sketch": [
    { "path": "orders/returns.play", "source": "module Orders\n  feature Returns\n    slice StateChange RegisterReturn\n..." }
  ]
}
```

Each document replaces the one at its path, or is added when there is none. The
sketch is drawn over the documents on disk exactly as a proposal is, with the same
*Current* and *Proposed* switch and the same summary. It is never validated as a
proposal, never kept, and never written. A sketch that does not compile is drawn
as far as it can be read, with the number of errors shown on the board. Pass
either `proposalId` or `sketch`, not both.

Ask for the board again after each change to the sketch or the model: every call
draws a new board, from the model as it is at that moment.

## Compare with the VS Code extension

| | MCP board | VS Code extension |
| --- | --- | --- |
| Opens from | A prompt in the conversation | A `.play` file |
| Follows your edits | Draws again on request | Redraws as you type |
| Shows a proposal or sketch | Yes | No |
| Needs the MCP server | Yes | No |

Use the extension while you write, and the MCP board to review what an assistant is
about to write.
