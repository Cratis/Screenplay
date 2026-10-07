# The AI corpus (`Cratis/AI`)

The shared corpus in https://github.com/Cratis/AI is meant to make agents masters of model-first
development: event modeling, the Screenplay language, the Screenplay MCP server and the `cratis` CLI. It
repeats facts from this repository: keywords, syntax, diagnostic codes, tool names, versions. Every language,
MCP or CLI change must leave it correct and complete.

## Rules

- Never edit `.cratis/ai/skills/cratis-*`, `.cratis/ai/rules/*.md` (outside `project/`), agents or other managed
  files in this repository. They are copies from `Cratis/AI`; `cratis ai status` reports the drift and
  `cratis ai update` refuses it without `--force`. The fix belongs in `Cratis/AI`.
- Do not copy this repository's AI tree into another repository, or the corpus into this one.
- The corpus is authored in `Cratis/AI` under `.cratis/ai/` (skills, agents, prompts, `mcp-servers.json`,
  `profile-catalog.json`). A skill's checks live next to it in `verification.json`.
- The pull request comment (not the release-note body) carries one line:
  `Corpus impact: <Cratis/AI issue or pull request URL>` or `Corpus impact: none - <reason>`.
  A reason such as "internal refactor, no behavior change" is fine; "will do later" is not.

## Which skill teaches which construct

Paths are `.cratis/ai/skills/<skill>/` in `Cratis/AI`; each has a `SKILL.md` and often `references/`.

| Change touches | Skills to check |
| --- | --- |
| `command`, `identifier`, `reads`, `validate`, `authorize`, `produces`, `handler`, `concurrency`, `event`, `constraint`, `policy`, `persona`, `concept`, `type`, `seed`, `$context` | `cratis-screenplay-command-surface` (has `references/context.md`) |
| Projections, `from` / `every` / `all`, joins, children, counters, `reducer` | `cratis-screenplay-projections` (`references/pdl-grammar.md`) |
| `readmodel`, `query`, `screen`, name resolution | `cratis-screenplay-read-surface` |
| `layout`, `arrangement`, templates, `form`, `contribute`, `behavior`, `ui profile`, `theme`, `$strings`, `file` | `cratis-screenplay-ui-composition` |
| `capture`, `reaction`, `trigger`, clock | `cratis-screenplay-captures-and-reactions`, `cratis-screenplay-automations-and-translations` |
| `specification`, given / when / then, reference execution | `cratis-screenplay-specifications`, `cratis-screenplay-scenario-coverage` |
| Event sources, streams, generations, evolution | `cratis-screenplay-streams-and-consistency` |
| Slice shape, refusals, field lineage | `cratis-screenplay-slice-design` |
| Domain discovery vocabulary | `cratis-screenplay-discovery`, `cratis-screenplay-event-modeling` |
| Review checks, anti-patterns | `cratis-screenplay-model-review` |
| PLAY codes, ESM versions and admission, compiler and CLI versions, verdict commands | `cratis-screenplay-toolchain` (`references/versions.md`, `diagnostics.md`, `executable-subset.md`, `renderable-subset.md`, `traps.md`, `cheat-sheet.md`) |
| MCP tools, arguments, descriptions, tool count, readiness, repair, layout | `cratis-screenplay-model-authoring` (`references/mcp-tools.md`, `mcp-loop.md`, `language-reference.md`) |
| Render admission, Stage behavior | `cratis-screenplay-render-and-gap-fill` |
| Extraction from code | `cratis-screenplay-legacy-extraction` |
| The whole-method flow, phases, verdicts | `cratis-screenplay-modeling-lifecycle` |

Also check, in `Cratis/AI`:

- Agents `.cratis/ai/agents/screenplay-modeler.md`, `screenplay-reviewer.md`, `screenplay-renderer.md`, and
  the Screenplay gating text in the generic agents that mention it.
- `.cratis/ai/mcp-servers.json`: the `screenplay` server entry (currently `cratis screenplay mcp`).
- `.cratis/ai/profile-catalog.json`: the `cratis/screenplay` skill list when a skill is added or renamed.
- Prompts that mention Screenplay (`new-vertical-slice.prompt.md`, `add-reactor.prompt.md`, `write-specs.prompt.md`).

## What to update in each skill

1. **Examples.** Corpus fences are tagged ```` ```screenplay ````. Replace any example that uses a removed or
   deprecated form, and add one for a new construct, written so it compiles with
   `dotnet run --project Source/DotNET/Tool -- --warnaserror <file>.play`. Run that command on the corrected
   example in this repository before putting it in the issue.
2. **`verification.json`.** Each skill has assertions of kind `skill-contains` that require literal strings
   in `SKILL.md`. A renamed code, version or tool must be renamed there too, or the check pins stale text.
3. **Toolchain facts.** Pinned Screenplay tool and `cratis` CLI versions, the ESM versions each admits,
   PLAY and `STAGE-ESM-*` codes, verdict commands (`cratis-screenplay-toolchain/references/versions.md`).
4. **MCP.** Tool names, arguments, the tool count (and the extra tool on MCP-Apps hosts), readiness and
   `executableReady` wording, and any `PLAY0268` statements.
5. **Deprecations.** Forms that now warn (such as `PLAY0397`) must disappear from examples and traps.
6. **New coverage.** A new construct needs a home: an existing skill section, or a new reference file. Say
   which in the issue.

## Issue template

File with the steps in [other repositories](other-repositories.md); title
`Screenplay <short change>: update <skills>`. Body:

```markdown
Screenplay pull request: https://github.com/Cratis/Screenplay/pull/<n>
Released in: Cratis.Screenplay <version> (cratis CLI <version> once bundled)

## Change
<one paragraph in plain terms: what the language, diagnostic, tool or command now does>

## Skills to update
| Skill | File | Change |
| --- | --- | --- |
| cratis-screenplay-<name> | SKILL.md, references/<file>.md | <exact edit> |

## Corrected examples
<each affected example, old form and new form, in ```screenplay fences, compiled with
`dotnet run --project Source/DotNET/Tool -- --warnaserror`>

## Facts that moved
<PLAY codes, tool names, versions, counts, verification.json assertions that must change>
```

## Backstops that do not exist yet

Today nothing checks the corpus against Screenplay: `Cratis/AI` verifies only that each skill contains the
strings its `verification.json` lists. Three pieces are planned, and until they ship, this checklist is the
only guard:

- https://github.com/Cratis/Screenplay/issues/500: a machine-readable contract (keywords, diagnostic codes,
  MCP tools and arguments, CLI commands, ESM versions) published with each release.
- https://github.com/Cratis/AI/issues/529: the corpus checks itself against the latest Screenplay release and
  opens an issue on drift.
- https://github.com/Cratis/AI/issues/416: skill examples are compiled.

When they land, reduce this section to what they cannot see: new constructs that need new teaching, not just
corrected text.
