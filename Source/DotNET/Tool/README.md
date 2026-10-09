# Cratis Screenplay Tool

Compile and verify `.play` models, or connect an AI host to your model through the
Screenplay MCP server. MCP Apps-capable hosts can show the event model board
without writing a file.

![The Screenplay MCP App shows a proposed Notifications module beside Commerce's fulfillment flows. Its SendOrderConfirmation command produces OrderConfirmationSent; the surrounding frame identifies the local capture harness.](https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/images/mcp-app-what-if-harness.png)

*Captured in a minimal MCP Apps test host (a local capture harness), not a
commercial chat host. The capture uses a single-file Commerce adaptation and an
unsaved what-if sketch.*

## Quickstart

1. With .NET 10 installed, run `dotnet tool install --global Cratis.Screenplay.Tool`.
2. Validate your model: `screenplay ./specifications --warnaserror`.
3. Configure your MCP host to launch `screenplay mcp ./specifications`, then ask it to call `visualize-model`.

Use an existing physical model directory. The server uses stdio; starting it in
a terminal does not open a browser. For VS Code, add `.vscode/mcp.json`:

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

For other hosts, use their stdio configuration with the same command and arguments.
The board requires tool **4.47.0 or later** and a host advertising
`io.modelcontextprotocol/ui`. Without that capability, the server offers its
ordinary tools but not `visualize-model`.

## See the model before changing it

- Call `visualize-model` with `{}` to see the application on disk.
- Pass `proposalId` to compare an outstanding proposal with the disk model.
- Pass whole documents in `sketch` to see a what-if without retaining or writing it.

Sketch paths are relative to the model root. Multi-file sketches require tool
4.59.1 or later; older versions such as 4.48.0 have sketch-path failures. Never
pass both `proposalId` and `sketch`. Viewing does not apply a proposal. Keep
approval enabled for `apply` and `recover-workspace`.

The board parses and draws; it does not execute commands or specifications.
Run the compiler to check validity, and read diagnostics rather than treating a
partial board as success.

[See your event model](https://cratis.io/screenplay/see-your-event-model/) ·
[Install and configure MCP](https://cratis.io/screenplay/mcp/install/) ·
[MCP board guide](https://cratis.io/screenplay/mcp/view/) ·
[Compiler reference](https://cratis.io/screenplay/tool/)

## Record declared processing purposes

Run `screenplay report processing ./specifications --format json` for a controller inventory, or choose `markdown` (the default) or `csv`. Supply `--controller-name` and `--controller-contact` explicitly. The inventory is generated from model declarations, not legal advice or evidence that runtime protection is installed. MCP hosts can page the same facts with `processing-record`.

## CLI alternative

CLI hosting shipped in 3.11.0 as `cratis screenplay mcp ./specifications`.
For the board, use **CLI versions that bundle Screenplay 4.47.0 or later**;
no second global tool installation is needed. CLI validation uses
`cratis screenplay validate ./specifications --warnings-as-errors` rather than
the standalone tool's `--warnaserror` flag.

Licensed under the MIT license.
