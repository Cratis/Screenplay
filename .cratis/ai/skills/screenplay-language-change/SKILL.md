---
name: screenplay-language-change
description: "Land a Screenplay language change on every surface in one pull request: the C# and TypeScript compilers, diagnostics, executable semantic model, printer, layout, repairs, reference evaluator, event model board, MCP server, CLI, Monaco, the VS Code extension, documentation, samples and decisions, then raise issues in the other repositories that must adapt, above all the `Cratis/AI` skill corpus. Use when adding, changing, deprecating, renaming or fixing a construct, keyword, grammar rule, diagnostic, semantic or admission rule, printer output, repair, MCP tool or description, CLI command or editor feature in this repository. Not for: writing or reviewing a `.play` model (use the `cratis-screenplay-*` skills), choosing a compiler or reading a verdict (use `cratis-screenplay-toolchain`), or a change that touches no language behavior."
license: MIT
---

# Screenplay language change

The language is implemented in more than one place. A change that reaches only the C# compiler leaves the
TypeScript compiler, the editors, the documentation and the AI corpus describing a different language.
This skill lists every surface, how to update it and how to verify it.

This skill is project-owned. It is not in `.cratis/ai.manifest.json`, so `cratis ai update` leaves it alone.
Never edit the managed `cratis-*` skills in this repository (see [the AI corpus](references/ai-corpus.md)).

## 1. Is this a language change?

It is when the change alters what a `.play` author can write, what the tools say about it, or what it means.
Any change under these paths qualifies unless it is a pure refactor with no observable difference:

- `Source/DotNET/Screenplay` (lexer, parser, syntax tree, binder, diagnostics, semantics, printer, files, workspaces)
- `Source/DotNET/Screenplay.Mcp`, `Source/DotNET/Tool`, `Source/DotNET/Screenplay.CanonicalCorpus`
- `Source/Screenplay/Compiler`, `Source/Screenplay/Monaco`, `Source/Screenplay/VSCodeExtension`,
  `Source/Screenplay/EventModels`, `Source/Screenplay/McpApp`
- `Documentation/screenplay` where it describes language behavior, and `Samples/`

Kinds of change: new or changed construct, keyword or grammar rule; a diagnostic that starts or stops firing,
or changes severity; a semantic or admission change (ESM version, `PLAY0268`); printer output; repair or
rename behavior; an MCP tool, argument or description; a CLI command or flag; an editor feature; a
deprecation, removal or rename; a bug fix that changes what compiles or what a diagnostic says.

If unsure, treat it as a language change and walk the checklist; a surface with nothing to change takes
seconds to rule out.

## 2. The rules

1. **Every surface in this repository is fixed in the same pull request.** "Follow-up" is not allowed for
   them. A surface that genuinely cannot support the change yet must reject or diagnose it explicitly, and
   the documentation says so.
2. **Every other repository that must adapt gets a GitHub issue** (or pull request), created when the change
   is made and linked from the pull request. `Cratis/AI` is first and most important. See
   [other repositories](references/other-repositories.md).
3. **Add exactly one corpus-impact label:** `ai-corpus: tracked` or `ai-corpus: none`.
   For `tracked`, link an existing `Cratis/AI` issue or PR in a pull request comment, or link back from that
   AI issue or PR. For `none`, add a comment line `Corpus impact: none - <reason>`; deferring work is not a
   reason. Add the evidence first and the label last. The `ai-corpus / verify` gate enforces this decision;
   keep the release-note body free of internal status.
4. **Diagnostic codes are permanent.** Never reuse or renumber a `PLAY` code.
5. **Samples never carry deprecated syntax or a form the compiler warns about.** See
   `.cratis/ai/rules/project/samples.md`.
6. Do not hand-edit generated vectors or goldens. Change the source, regenerate, review the diff, then rerun
   without the regeneration variable.

## 3. In-repository surfaces

Work through the table. Paths, specs and regeneration commands are in
[surfaces](references/surfaces.md); read the entry for each surface the change touches.

| # | Surface | Touch it when |
| --- | --- | --- |
| 1 | Syntax tree, walker, directive locations | A new or changed node or member |
| 2 | Parser and parse-time validators | Any new form, keyword or line grammar |
| 3 | AST JSON schema (`syntax-schema`) and transport golden | A node kind or member changed |
| 4 | Diagnostic codes (C#, TypeScript, Monaco) and `diagnostics.md` | A code added, retired or re-worded |
| 5 | Binder and semantic validators, ESM admission (`PLAY0268`) | The construct has executable meaning or is syntax-only |
| 6 | Executable semantic model versions and golden vectors | Admitted constructs or serialized bytes change |
| 7 | Reference evaluator and specification runner | Runtime meaning changes |
| 8 | Canonical printer, exact-source and trivia round trip | Any syntax change |
| 9 | Folder merge, imports, layout, `recommend-layout` | Placement or merge rules change |
| 10 | Repairs | A diagnostic gains or loses a quick fix |
| 11 | Rename and refactoring | A declaration or reference kind is added |
| 12 | Dependency graph and event model board | A construct appears on the board or in dependencies |
| 13 | MCP server tools, descriptions, instructions, MCP reference docs | A tool, argument, description or count changes |
| 14 | CLI tool | A command or flag changes |
| 15 | Contexts package | The context or identity model changes |
| 16 | Canonical corpus and vectors | Corpus documents should exercise the construct |
| 17 | TypeScript compiler and conformance vectors | Any construct the TypeScript compiler reads |
| 18 | Monaco language service and bundled editor sample | Highlighting, hover, completion, validation |
| 19 | VS Code extension: TextMate grammar, features, native editor | Any new word, form or diagnostic |
| 20 | Documentation | Always for a visible change |
| 21 | Samples | Always for a visible change |
| 22 | Decision record | A ruling on meaning, scope or admission |
| 23 | Spell lists | A new keyword or term |

## 4. Other repositories

Open the table in [other repositories](references/other-repositories.md), decide for each row whether the
change triggers adaptation, and run the `gh issue create --repo Cratis/<repo>` step for each that does. Search
for an existing issue first (`gh issue list --repo Cratis/<repo> --search "<keyword>"`) and link it rather
than filing a duplicate. Put each URL in the pull request comment.

## 5. The AI corpus (`Cratis/AI`)

The corpus teaches agents to work model-first with Screenplay, its MCP server and the CLI. A language change
that leaves it stale makes agents write models the compiler rejects or miss new constructs entirely.
[The AI corpus reference](references/ai-corpus.md) maps each construct to the `cratis-screenplay-*` skills
that teach it, lists what to update (examples, `verification.json` assertions, toolchain versions, PLAY codes,
MCP tool list and count, agents, `mcp-servers.json`) and gives the issue template.

Short form: open a `Cratis/AI` issue naming the exact skill changes with corrected `screenplay` examples;
never edit the managed `.cratis/ai/skills/cratis-*` copies here.

The backstops that would catch drift automatically are not in place yet: a machine-readable contract
(https://github.com/Cratis/Screenplay/issues/500), a corpus drift check
(https://github.com/Cratis/AI/issues/529) and compiling skill examples
(https://github.com/Cratis/AI/issues/416). The `ai-corpus / verify` gate checks that a decision and its
evidence exist, not whether the corpus is correct; until those backstops land, review must catch drift.

## 6. Local verification

Mirror CI (`.github/workflows/dotnet-build.yml`, `javascript-build.yml`). Run each phase on its own, and
only the affected ones while iterating; run the full set before reporting done. For changes to the corpus
impact gate, run `node --test .github/scripts/tests/*.test.mjs` and `actionlint` if available. After editing
comment evidence, re-run the failed gate (`gh run rerun <run-id> --failed --repo Cratis/Screenplay`): comment
edits do not trigger it, but a re-run reads live state.

```bash
yarn                                                    # once
yarn workspaces foreach -Rt --from @cratis/screenplay-mcp-app run build   # MCP packing needs the board page
dotnet test --configuration Debug                       # compiler, MCP, Tool, samples, documentation specs
dotnet build --configuration Release -p:Version=9999.0.0   # whole solution, CI warning settings
dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj --configuration Release --no-build --no-restore
dotnet pack --no-build --configuration Release -o ./Artifacts/NuGet -p:Version=9999.0.0
python3 Source/DotNET/Screenplay.Contexts/verify-package.py ./Artifacts/NuGet 9999.0.0
yarn build                                              # then, each as its own phase:
yarn compile
yarn lint:ci
yarn test
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing
```

## 7. Done checklist

Tick each before reporting a language change complete. "N/A" needs a reason you can state in one line.

- [ ] Syntax tree, parser, walker and AST schema updated; schema and collection goldens regenerated and reviewed.
- [ ] Diagnostic code added in C#, TypeScript and Monaco lists; listed in `Documentation/screenplay/diagnostics.md`.
- [ ] Binder, validators and ESM admission updated, or the construct is refused by an explicit binder disposition with a specification, and documented.
- [ ] ESM goldens regenerated when serialized bytes changed; `Screenplay.CanonicalCorpus` checked.
- [ ] Printer prints the construct and the round-trip specs hold.
- [ ] Folder merge, layout, repairs, rename and reference evaluator updated or ruled out.
- [ ] Event model board and `McpApp` map the construct, or a spec shows why not.
- [ ] MCP tool descriptions, instructions and `Documentation/screenplay/mcp` reference match; CLI docs match.
- [ ] TypeScript compiler reads the construct; `Conformance` documents and `diagnostics.json` updated.
- [ ] Monaco: tokens, keyword docs, hover, completion, validation; bundled `invoicing.play` copy in step.
- [ ] VS Code: TextMate grammar, language features and native editor; extension specs pass.
- [ ] `Documentation/screenplay` page and `grammar.md` updated; docs specs pass.
- [ ] `Samples/Invoicing` (and any natural sample) uses the construct; sample READMEs current.
- [ ] Decision record written or updated when the change is a ruling.
- [ ] cspell lists updated for new words.
- [ ] Every section 6 phase passes, as CI runs it: `dotnet test` (Debug), Release build, CanonicalVectors Release tests, `dotnet pack` with `verify-package.py`, and `yarn build`, `compile`, `lint:ci`, `test`.
- [ ] Issues opened or linked for every affected repository, `Cratis/AI` first.
- [ ] Exactly one `ai-corpus:` label added last: `tracked` with a verified `Cratis/AI` comment link or
  back-reference, or `none` with a `Corpus impact: none - <reason>` comment; `ai-corpus / verify` passes.
  The pull request body is release-note only.
