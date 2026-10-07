# Screenplay MCP distribution and discovery

Screenplay is a **local-first** stdio MCP server. Every distribution uses
`ScreenplayMcpServer` from this project; the executable is the existing
`Cratis.Screenplay.Tool`. Bundles do not duplicate server logic or introduce a
hosted proxy. Docker, NuGet libraries and the .NET tool remain supported.

## Ownership and release order

The Cratis Screenplay release maintainer owns packages, release assets, signing
policy and directory submissions. A Cratis organization owner owns publisher
verification, directory accounts and reviewer communications. Do not submit as a
personal publisher, invent an assigned app ID, or publish credentials in source.

1. Merge the Screenplay feature with the appropriate semantic release label.
   `publish.yml` cuts one version and publishes NuGet/.NET, Docker, npm, the VS Code
   extension and the desktop artifacts. Desktop packaging must finish successfully;
   a GitHub tag existing is **not** proof the packages are available.
2. Wait for **all** publish jobs, including `publish-desktop`, to succeed. Check
   every artifact and checksum on the exact GitHub release. Download and test the
   macOS and Windows bundles in the actual desktop clients before claiming their
   UI installation has been verified.
3. Only then merge/release CLI desktop-install support. Its artifact names and
   checksums must match this release contract. Consumers resolve Screenplay
   releases independently of the CLI's embedded library version.
4. Submit/update curated listings through the manual approval flows below.
   Until accepted, public docs must say that listing is pending and offer the
   direct release download. Never link to an invented directory listing.

## Build and validate locally

Prerequisites: .NET 10 SDK, Node 24, Corepack/Yarn, Python 3.12 or later, and
`@anthropic-ai/mcpb` **2.1.2**. Python validation uses `jsonschema` **4.26.0**.
These are maintainer build tools, not end-user dependencies.

From the repository root (use a Python virtual environment):

```bash
npm install -g @anthropic-ai/mcpb@2.1.2
python -m pip install jsonschema==4.26.0
corepack enable
yarn install
yarn workspaces foreach -Rt --from @cratis/screenplay-mcp-app run build
python -m unittest discover -s Source/DotNET/Screenplay.Mcp/Packaging -p 'test_*.py'
dotnet publish Source/DotNET/Tool/Tool.csproj -c Release -r osx-arm64 --self-contained true -p:PackAsTool=false -p:Version=4.55.0 -o Artifacts/Desktop/publish
python Source/DotNET/Screenplay.Mcp/Packaging/build.py --publish Artifacts/Desktop/publish --output Artifacts/Desktop/packages --version 4.55.0 --rid osx-arm64
python Source/DotNET/Screenplay.Mcp/Packaging/smoke.py Artifacts/Desktop/packages/*.mcpb Artifacts/Desktop/packages/*-plugin.zip
```

Replace the example version with the release version and the RID with the native
platform. Build on that platform before running smoke tests. `mcp-desktop.yml`
covers `osx-arm64`, `osx-x64`, `win-x64`, `linux-x64`, and `linux-arm64` using native
runners. Linux packages are for compatible local MCP hosts; neither Claude nor
ChatGPT Desktop support on Linux is claimed. Windows arm64 is not packaged.

The event model board is built **before** the .NET assembly that embeds it.
Publishing is self-contained (the runtime is included), not trimmed, and preserves
all tool behavior. The release version is shared by assemblies, manifests and
filenames. CI checks portable schemas, runs `mcpb validate`, packs with `mcpb pack`,
unpacks both packages and exercises MCP initialization, tools, resources and a
representative application read against the real binary.

The vendored `Packaging/Schemas` files are Agent Plugins **1.0.0** schemas from
[agent-plugins.org](https://agent-plugins.org). Update them deliberately alongside
format changes; validation does not silently follow a moving remote schema.
OpenAI presentation metadata is additionally reviewed against its current field
reference (the portable schema leaves vendor extensions open).

## Artifact contract

Each release attaches these names for each supported RID:

- `screenplay-VERSION-RID.mcpb`
- `screenplay-VERSION-RID-plugin.zip`
- Each file's exact name plus `.sha256`, containing SHA-256, two spaces, and the
  filename on one line.

Release assets are under `https://github.com/Cratis/Screenplay/releases` and are
uploaded only after all native desktop jobs pass. CI verifies checksums again
before upload. The CLI downloads only from that Cratis-owned release source and
checks cached downloads too. HTTPS and same-release hashes provide transport and
artifact integrity; **they are not a publisher signature**. Do not describe these
assets as signed or notarized.

## Claude Desktop: MCPB

`Packaging/build.py` produces manifest **0.3** with `server.type = "binary"`, a
platform-specific entry point and a stdio launch of the bundled tool as `mcp` with
no root argument and no `user_config`. The server binds its workspace dynamically:
a single MCP root offered by the host, else the working directory when it holds
`.play` files, else the folder the assistant passes to `open-workspace`. For a single
client-offered project, existing `.screenplay/identities.json` or `.screenplay/pending.json`
on the path to the discovered model folder keeps its state directory as the root.
Competing state is reported even when opening fails; any pending journal on that
path blocks opening, reads and writes until explicit recovery at the journal's own root.
The icon is Cratis' existing Screenplay PNG. MCPB validation checks manifest and icon compatibility.

To test in the desktop app:

1. Download the native MCPB and verify its `.sha256` file.
2. Update Claude Desktop. Open **Settings → Extensions → Advanced settings →
   Extension Developer → Install Extension**, and choose the MCPB. Opening the
   file through the OS is a convenience; this documented UI is the fallback when
   file association is unavailable.
3. Review trust/permissions, then install/enable. Name a model folder in the
   conversation so the assistant opens it. Ask it to describe the application, list tools and, where MCP
   Apps is enabled, show the event model board.
4. Propose a small change, review before `apply`, restart, and confirm persistent
   identities. Test a folder without `.play` files and denied filesystem write as negative cases.
5. Install a newer bundle and confirm approvals and model remain correct. Remove it from **Settings → Extensions**. The CLI cannot inspect
   host-private extension storage or prove installation through a public API.

Team/Enterprise allowlists or device policy can block sideloading; respect those
policies. There is no documented unattended installation/management API used here.

### Signing decision

The initial release bundles are **unsigned**. There is no certificate provisioned
in this workflow. Self-contained executable signing/notarization (Apple Developer
ID and Windows Authenticode) is distinct from MCPB bundle signing. Before offering
signed production bundles, the publisher owner must provision approved certificates
and secure key storage; do not use a generated self-signed certificate as publisher
verification. Use the MCPB CLI's documented `sign`, `verify` and `info` operations,
verify the signed bundle, then regenerate its checksum **after signing** and repeat
smoke/UI tests. Never check keys, certificate passwords or reviewer credentials in.
See [MCPB signing](https://github.com/modelcontextprotocol/mcpb/blob/main/CLI.md).

### Claude curated extension directory

Current authoritative flow:
[Claude local MCP servers](https://support.claude.com/en/articles/10949351-getting-started-with-local-mcp-servers-on-claude-desktop)
links the [Desktop Extension Submission Form](https://clau.de/desktop-extention-submission)
(the spelling in that official URL is intentional). Local desktop extensions use
this form, **not** the remote connector/plugin developer portal.

The publisher owner signs in and completes the live form. Prepare:

- Cratis publisher identity/contact and rights to all bundled code and branding;
- stable `cratis-screenplay` identity, version, description, source/homepage,
  license and exact native bundle download links;
- PNG icon, clean screenshots of install/root selection and event model board,
  and a walkthrough with a nonconfidential sample model;
- security/privacy explanation: local process permissions, chosen model root,
  no hosted Screenplay service, host model-provider data handling, tool approvals,
  proposal/apply boundary, identity persistence and recovery;
- supported OS/architectures, self-contained runtime, checksum/signing status,
  dependencies/license notices, test instructions and reviewer sample data;
- compliance with [Anthropic Software Directory Terms](https://support.claude.com/en/articles/13145338-anthropic-software-directory-terms)
  and any current form requirements.

Form fields and approval requirements are controlled by Anthropic; check them at
submission time, rather than treating this checklist as proof of acceptance. Record
the submission confirmation, assigned listing identifier, owner and reviewer
requests in Cratis' release tracking. For updates, use the existing listing and
publisher account, supply the new version/bundles and release notes through the
submission/reviewer channel, and follow the reviewer's update instructions. Directory
updates can be automatic for users; privately downloaded bundles require reinstall.
Do not announce availability until the listing is actually live.

## ChatGPT Desktop: local Agent Plugin

The plugin ZIP has root `plugin.json`, root `mcp.json`, `skills/screenplay/SKILL.md`,
Cratis branding and the same self-contained binary. The stdio command is a single
**plugin-relative token**, `./server/Cratis.Screenplay.Tool` (with `.exe` on Windows).
Commands do not interpolate `${PLUGIN_ROOT}`; this follows the portable specification.
The arguments are just `mcp`. `screenplay mcp ROOT` still requires an existing root, and
`mcp --create-root DIRECTORY` creates one. `screenplay mcp` with no root starts a dynamic server that binds a
root on first use: `open-workspace` `path`, then the client's single `roots/list` root (re-read on
`notifications/roots/list_changed`; the server serves the model inside that project: its `.play` files,
else `Source`/`src`, else a new `Screenplay` folder), then the working directory when it holds `.play`
files or `.screenplay`, and finally `Documents/Screenplay` in the user's home folder, created on demand.
Existing `.screenplay/identities.json` or `.screenplay/pending.json` between the offered
project and discovered model folder (inclusive) keeps its state directory as the root,
without creating a fallback folder. With several state roots, the nearest to the offered
root wins and `rootBindingConflict` names all roots and pending journals, including on
failed opens. Default-bound opening, reads and writes are refused while any state root
on that path holds a pending journal; open its root with an explicit `path`, inspect
`workspace-state`, then explicitly recover its journal.

For an existing model, the CLI installer supplies `--model-root DIRECTORY` and
configures the owned package's arguments to launch that directory without creation.
There is no automatic trust, no hosted proxy, and no change to project-local Codex or
Claude Code configuration. The host may load a skills-only plugin but not support
stdio on a given platform/version; only claim local MCP operation after checking
the installed app's Plugins capability. macOS/Windows CI verifies native server
execution, not the interactive ChatGPT UI or subscription eligibility.

### Personal or repository marketplace testing

Prefer `cratis screenplay mcp install --clients chatgpt --model-root ABSOLUTE_ROOT`
once the coordinated CLI release is available. Restart ChatGPT and install/enable
Screenplay under **Plugins**. The CLI registers a source; it does not bypass the
host's consent or claim installation was verified.

For manual development, unpack the ZIP into `plugins/cratis-screenplay` in a test
repository. Copy the generated `Artifacts/Desktop/packages/marketplace.json` to
that repository's `.agents/plugins/marketplace.json`. It uses
`source.source = "local"`, `source.path = "./plugins/cratis-screenplay"`, and policy
`AVAILABLE`/`ON_INSTALL`. Relative paths resolve from the **repository root**, not
from `.agents/plugins`. Preserve other entries if a marketplace already exists.
Restart ChatGPT and install/enable the local plugin. Check tools, model reads,
proposal approvals and persistence as in the Claude checklist.

For a personal marketplace, store the extracted plugin under `~/.codex/plugins/`
and add its absolute path to `~/.agents/plugins/marketplace.json`, preserving the
marketplace name and other entries. Those locations follow OpenAI's documented
source mechanism; do not write host-internal plugin caches. Direct plugin downloads
start with an empty host-owned model unless their owned MCP arguments are configured
for an existing one. The CLI does that without requiring manual JSON authoring.

### OpenAI public directory: blocked on local MCP approval

The current [package guide](https://developers.openai.com/plugins/build/plugins)
says public MCP submissions require a **public HTTPS endpoint**; developers unable
to provide one must contact OpenAI for local MCP support. Screenplay cannot become
remote solely to pass that requirement: its purpose is local model files.

The Cratis publisher owner must engage the organization's OpenAI contact (or the
support channel referenced in the developer portal) with the local package, security
model and reviewer walkthrough. Request local-binary distribution/review support,
record the response and requirements in release tracking, and do not fake an HTTPS
endpoint or submit a skills-only listing that claims the local server is connected.
No OpenAI app/plugin ID is assigned by this package. If one is assigned, record it
in publisher-owned release configuration and approved vendor metadata, not a
hard-coded developer-specific identifier.

Once OpenAI confirms an eligible local MCP submission path, follow its instructions
and the current [submission guide](https://developers.openai.com/plugins/deploy/submission):

1. Use the Cratis organization/project. An owner or a member with **Apps Management
   Write** completes business/individual verification and selects the verified
   developer identity at `https://platform.openai.com/plugins`.
2. **Upload new or existing plugin → Upload plugin** with the complete ZIP including
   MCP configuration in the initial submission. Resolve package, metadata and skill
   findings in **Metadata & Skills**. Public metadata URLs must identify Cratis.
3. Complete the **MCPs** setup required by the approved local-MCP route. The ordinary
   route asks for HTTPS, domain verification and authentication; it is not usable
   for this stdio package without OpenAI's explicit local support.
4. Prepare the current required icons/screenshots, publisher/privacy/terms URLs,
   five positive and three negative review cases, video walkthrough and release
   notes. Run cases with sample data. Supply reviewer credentials separately if
   required; never in the ZIP. Include platform/root selection and safe refusal cases.
5. Complete **Review information → Review details**, policy attestations, and
   **Submit for review**. Track **Review status** and email feedback. Only one review
   can be active; address rejection findings or appeal by replying to the email.
6. After approval choose **Publish plugin**. For later metadata/skill/binary updates,
   upload a complete new ZIP to the **same plugin**, resolve findings, review and
   publish its approved version. Do not assume remote daily MCP scans update local
   executables. Document any local-specific update requirements OpenAI assigns.

This release provides the local package and discovery runbook, **not an approved
public listing** or a completed publisher contact. Automated tests cannot substitute
for those publisher-owned external steps.

## Existing distributions

- **Docker:** `docker run -i --rm -v ABSOLUTE_ROOT:/model cratis/screenplay:VERSION mcp /model`.
  Linux writers may need `--user` matching their host UID/GID. The existing publish
  workflow produces amd64/arm64 images and only updates `latest` for stable releases.
- **.NET tool:** `dotnet tool install --global Cratis.Screenplay.Tool --version VERSION`,
  then `screenplay mcp ABSOLUTE_ROOT`. Update with `dotnet tool update --global`.
- **Embedding:** consume `Cratis.Screenplay.Mcp` and call `ScreenplayMcpServer.Run`.
- **Manual client configuration:** register the tool or container over stdio, leave
  stdout protocol-only and keep approval for `apply` and `recover-workspace`.

See the [public installation guide](../../../Documentation/screenplay/mcp/install.md),
[MCPB manifest](https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md),
[MCPB CLI](https://github.com/modelcontextprotocol/mcpb/blob/main/CLI.md),
[Agent Plugins specification](https://agent-plugins.org), and
[ChatGPT Plugins help](https://help.openai.com/en/articles/20001256-plugins-in-chatgpt).
