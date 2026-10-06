---
name: screenplay
description: Explore and author a local Screenplay business model using the Screenplay MCP tools.
---

Use the Screenplay MCP server to discover tools and inspect the application before editing it.
One model root is one application. Without a project the server works in `Documents/Screenplay` in the user's home folder;
pass `path` to `open-workspace`, or use the Cratis CLI desktop installer with `--model-root`,
to work in another existing directory. If the root has no `.play`
files, application-read tools can report that no model exists yet; open the
workspace and use syntax schemas to propose its first documents instead.

Read diagnostics, source revisions, and declaration identities. Propose changes, explain the
proposal, and ask the user to review before calling `apply`. Keep approval enabled for `apply`
and `recover-workspace`. Never delete identity state or pending-operation markers to bypass
recovery. Render the event model board only if the host advertises MCP Apps support.

The server runs locally with the user's filesystem permissions. Do not send a model to a
hosted proxy or assume that plugin installation grants approval to change files.
