# Other repositories that consume the language

A language change in this repository does not reach these repositories on its own. Each consumes the
published `Cratis.Screenplay` packages (or, for the corpus, this repository's documented behavior). For every
row, decide whether the change triggers adaptation; if it does, file the issue and link it from the pull
request comment.

Paths below were checked against sibling checkouts; recheck the repository when the issue is written.

| Repository | What consumes the language | The change triggers adaptation when |
| --- | --- | --- |
| [Cratis/AI](https://github.com/Cratis/AI) | 18 `cratis-screenplay-*` skills, three agents, `mcp-servers.json`, profile catalog (see [the AI corpus](ai-corpus.md)) | Any construct, keyword, grammar, diagnostic, admission, MCP tool or CLI change. Almost every language change. |
| [Cratis/Stage](https://github.com/Cratis/Stage) | `Source/Rendering.Cratis`, `Source/Contracts`, `Source/Specifications`, `Source/Conformance.Specs` (package references to `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus`) | ESM or semantic change, canonical corpus change, a construct the renderer must render or refuse, an admission change (`STAGE-ESM-*`) |
| [Cratis/cli](https://github.com/Cratis/cli) | `Source/Cli/Commands/Screenplay` (`cratis screenplay ...`), bundles `Cratis.Screenplay`, the MCP server and the Arc, CritterStack, Prologue and Generation packages | New or changed CLI or MCP surface, a package version bump the CLI must take |
| [Cratis/Arc](https://github.com/Cratis/Arc) | `Source/DotNET/Screenplay`, `Screenplay.Embedded`, `Screenplay.Embedded.Generation` (the C#-to-Screenplay generator, with its own `SP` diagnostic codes) | A construct the generator should emit, a form it emits that is deprecated or removed, a diagnostic interop change |
| [Cratis/Screenplay.Generation](https://github.com/Cratis/Screenplay.Generation) | `Source/DotNET/Generation.DotNet*` (uses the syntax tree and printer to write `.play`) | Syntax tree or printer change, a construct worth generating |
| [Cratis/Studio](https://github.com/Cratis/Studio) | `Source/Core`, `Source/ScreenplaySessions` (`Cratis.Screenplay` and the canonical corpus, and the MCP session client) | Language, ESM or MCP protocol change |
| [Cratis/Prologue](https://github.com/Cratis/Prologue) | `Source/Screenplay` (`Cratis.Prologue.Screenplay`: extraction to syntax tree to `.play`) | Syntax tree or printer change, a construct extraction should produce |
| [Cratis/Screenplay.CritterStack](https://github.com/Cratis/Screenplay.CritterStack) | `Source/DotNET/CritterStack`, `Source/DotNET/Canonical` (Marten backend over the semantic model) | ESM or canonical corpus change that alters what the Marten backend must implement |
| [Cratis/Scene](https://github.com/Cratis/Scene) | Documents the object model for Screenplay screens; no package reference | Concept, screen or layout meaning changes (documentation only) |

Repositories with no Screenplay package reference (`Documentation`, `cratis.github.io`, `StudioIssues`) need
no issue for a language change. The Documentation site aggregates `Documentation/screenplay` from this
repository, so updating the pages here is enough.

## Creating the issue

1. Search first: `gh issue list --repo Cratis/<repo> --search "<construct or code>" --state open`. If an
   issue covers it, link that one and add a comment only if it lacks the new information.
2. Create it: `gh issue create --repo Cratis/<repo> --title "<what must adapt>" --body-file .ai-work/<task>/issue-<repo>.md`.
   Write the body to a file first.
3. The body is self-contained: the Screenplay pull request URL (absolute), the construct or code in plain
   terms, the old and new `screenplay` example, the exact change the repository must make, and the
   Screenplay package version that carries it.
4. Put every issue URL in the Screenplay pull request comment, one line per repository, with
   `Cratis/AI` first. The pull request body stays a release note.

Do not open a pull request in another repository unless the task asks for it. Assignment follows the global
rules for Cratis issues.
