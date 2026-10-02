---
title: See your event model
description: Open the event model board in VS Code, an AI chat, your Arc application, the CLI, or Studio, and generate a model from existing code.
---

Open a `.play` model and follow a feature from the screen to its command, events,
and read model. The same event model board is available in VS Code, an MCP App,
your running Arc application, the CLI's browser explorer, and Cratis Studio.
You do not need to redraw the diagram for each place.

**Allow about 10 minutes.** Start in VS Code, then choose the other place that
fits your work. You need VS Code with its `code` command on `PATH` and a `.play`
model. For a ready-made example, open the
[Commerce sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Commerce)
from a Screenplay checkout; keep the whole folder, not just one slice file.
The AI path needs .NET 10 and an MCP Apps-capable host. The code paths need a
compatible .NET SDK, restored project dependencies, and the
[Cratis CLI](/cli/getting-started/).

## Open the board in VS Code

1. Install the extension:

   ```bash
   code --install-extension cratis.screenplay
   ```

2. Open your model's folder in VS Code, then open a `.play` file. For Commerce,
   choose `Ordering/Orders/PlaceOrder.play`. The extension opens the board by
   default and resolves the surrounding application.
3. Choose **Show Source** in the editor title bar. Edit the text beside the board
   and watch it redraw. Pan to a slice and use **View** to show properties or
   switch between **Full** and **Overview**.

![Commerce's Ordering board beside the PlaceOrder source, with command, event, screen, and specification cards](images/vscode-source-and-board.png)

*The released extension in code-server, showing the Commerce sample.*

You have succeeded when you can find `PlaceOrder`, the `OrderPlaced` event it
produces, and the specifications below the slice. The text remains the source of
truth. See [VS Code extension](vscode.md) for editor associations and folder rules.

## Ask for the board in an AI chat

Install the standalone tool if you do not already have it:

```bash
dotnet tool install --global Cratis.Screenplay.Tool
screenplay mcp ./specifications
```

Replace `./specifications` with an existing physical model directory. This starts
a stdio server, not a browser; your host normally launches the command for you.
For example, put this in VS Code's `.vscode/mcp.json` when the workspace contains
that directory:

```json
{
  "servers": {
    "screenplay": {
      "type": "stdio",
      "command": "screenplay",
      "args": ["mcp", "${workspaceFolder}/specifications"]
    }
  }
}
```

Other hosts use their own configuration format with the same command and
arguments. Connect, then ask: **“Show my event model with visualize-model.”**
The host must advertise `io.modelcontextprotocol/ui`; without MCP Apps support,
`visualize-model` is not offered.

The board requires **Cratis.Screenplay.Tool 4.47.0 or later**, or **CLI versions
that bundle Screenplay 4.47.0 or later**. CLI hosting itself shipped in 3.11.0:
you can use `cratis screenplay mcp ./specifications` instead, with `command`
`cratis` and `args` `["screenplay", "mcp", "${workspaceFolder}/specifications"]`.
An older CLI can host MCP without offering the board. See
[Install the MCP server](install-mcp.md) for setup and recovery.

Use the same tool in three ways:

| You want to see | Call `visualize-model` with | What happens |
| --- | --- | --- |
| The model on disk | `{}` | Draws the application under the server root. |
| A proposed change against disk | `{"proposalId": "<proposalId>"}` | **Current** and **Proposed** switch between disk and the retained authoring proposal. |
| A what-if that writes nothing | A `sketch` array of whole documents, each with `path` and `source` | Overlays the scratch documents on disk for this view only; it does not retain or apply them. |

Ask the assistant to pass either `proposalId` or `sketch`, never both. A sketch
replaces the whole document at each supplied path, not a fragment of it. Start
with a self-contained, single-file model for what-if sketches: Screenplay 4.48.0
has known failures with multi-file sketch paths. Keep the original files intact.
The [MCP board guide](mcp-visualization.md) shows the argument shapes.

![The MCP App's Proposed board shows a Notifications what-if beside Commerce fulfillment flows inside a labeled capture harness](images/mcp-app-what-if-harness.png)

*The real MCP App rendered in a local capture harness, not a commercial chat
host. This capture uses a single-file Commerce adaptation and an unsaved
Notifications sketch.*

Viewing never applies a proposal. Keep approval enabled for `apply` and
`recover-workspace`, and review the exact plan before allowing writes.

## Open it in your running Arc app

For an ASP.NET Core application using Arc, add
`Cratis.Arc.Screenplay.Embedded` and follow Arc's
[embedded event-model setup](/arc/backend/csharp/embedded-event-model/).
Build and run the app, then open **`/.cratis/event-model/`** on its local address.
Select a project or feature; switch to **Source** to inspect its generated `.play`.

Keep the route **development-only**. Explicit mapping should be guarded by
`app.Environment.IsDevelopment()`. Automatic Cratis hosting detects a debug
build, not the environment name; a debug build deployed elsewhere can still
expose structure. The Arc guide covers disabling generation and protecting the
route. This board reads source-derived resources embedded during the build,
not stored events or live business data.

![Arc's runtime explorer shows four command slices in its AspNetCore sample, with project navigation and conversion warnings](images/arc-runtime-full-board.png)

*The Arc AspNetCore sample is not event-sourced: its event lanes are empty.
Conversion warnings remain visible.*

## View a project without starting its app

With CLI 3.23.0 or later, point the browser explorer at your project:

```bash
cratis view ./MyApp/MyApp.csproj
```

The CLI serves the explorer on loopback and opens your browser. It reads embedded
documents from built output (Debug by default), or generates them in memory when
none are available. Select a document to see the board. Stop the terminal process
with **Ctrl+C** when finished. Use `--from-source` to regenerate rather than read
embedded output, or `--no-browser` to open the printed URL yourself.

![The cratis view browser explorer lists AspNetCore and Shared projects beside the sample's board and conversion warnings](images/cli-view-explorer.png)

*The CLI and runtime routes use the same explorer; neither starts the business
application to inspect its model.*

## Share a public model with Studio

The anonymous viewer needs no account:

```text
https://view.cratis.studio/?url=<public .play URL>
```

Replace the placeholder with a URL-encoded, publicly accessible raw `.play` URL,
not a GitHub HTML file page. Use a complete standalone document supported by the
viewer. A Commerce slice is a fragment, and the deployed viewer does not support
the Commerce root's file imports. Do not expect either URL to load that sample.
Never put a private model URL, access token, or customer data into a share link.

For editing, sign in to [Cratis Studio](https://app.cratis.studio/). Import a
Screenplay `.play` file or a folder zip into an event model, review the import's
warnings, and accept only what you intend to keep. Export either a single `.play`
document or a folder zip to bring the model back to your editor. Review the
exported source: canvas import/export is not a lossless round trip for every
language construct.

## Get a model from existing code

If your code came first, generate the structure it already declares:

```bash
cratis screenplay generate ./MyApp/MyApp.csproj --provider arc --file MyApp.play
```

Use `arc` for Arc/Chronicle source, `marten` for Marten, or `critter-stack` for
Marten with Wolverine. Omit `--provider` for automatic detection. Generation
reads a Roslyn compilation; it does not start the app or connect to its event
store. Read the diagnostics before treating the output as complete. See
[Arc generation](/arc/backend/csharp/generating-a-screenplay/) and the
[Critter Stack guides](/screenplay/ecosystem-examples/critter-stack/).

Open the generated file in VS Code. In CI, validate it and fail on warnings:

```bash
cratis screenplay validate ./MyApp.play --warnings-as-errors
```

With no path, validation checks the current directory as one application. Use a
CLI bundling a compiler that understands your model's syntax. The standalone
alternative is `screenplay ./MyApp.play --warnaserror`; its flag is different.

## Know what the picture does not prove

- **A board is a visualization, not execution.** The editor and browser boards
  parse and draw the model; they do not run commands or specifications.
- **Drawing is not full validation.** Boards can draw partial models with errors.
  Use the compiler and CI validation for acceptance, not the presence of cards.
- **Generation is not full reconstruction.** It recovers recognized source
  declarations, not arbitrary handler logic, every UI detail, or runtime behavior.
  Diagnostics and conversion warnings describe gaps; do not hide them.
- **Versions matter.** A released surface can bundle an older compiler. MCP Apps
  support, CLI hosting, language support, and viewer support are separate checks.
- **Sharing exposes structure.** Keep runtime routes local and public viewer URLs
  public-safe. No viewing step here requires applying an AI proposal.

Next, [model your first feature](getting-started.md) or
[review a change with MCP](mcp-authoring.md).
