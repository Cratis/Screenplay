---
applyTo: "**/*"
---

## Language changes land on every surface

The Screenplay language is implemented in several places: the C# compiler, the TypeScript compiler, the
executable semantic model, the MCP server, the CLI, the Monaco language service, the VS Code extension, the
documentation and the samples. A change to the language reaches all of them in the same pull request.

This applies to any change that alters what a `.play` author can write, what the tools report about it or what
it means: constructs, keywords, grammar, diagnostics, executable-model admission, printer output, repairs,
rename, MCP tools and descriptions, CLI commands, editor features, deprecations, removals and bug fixes that
change what compiles. It covers code under `Source/DotNET/Screenplay*`, `Source/DotNET/Tool` and
`Source/Screenplay`, and the language documentation.

Load the `screenplay-language-change` skill before starting and follow its checklist. The non-negotiables:

- Every surface in this repository is updated in the same pull request. A follow-up is not allowed; a surface
  that cannot support the change yet rejects it with a diagnostic and the documentation says so.
- Other repositories that must adapt get a GitHub issue, created with the change and linked from the pull
  request. `Cratis/AI`, the skill corpus, comes first. Never edit the managed `cratis-*` skills here.
- Add exactly one `ai-corpus:` label: `ai-corpus: tracked` with an existing `Cratis/AI` issue or PR link in a
  pull request comment, or a back-reference from that AI issue or PR; `ai-corpus: none` with a comment line
  `Corpus impact: none - <reason>`. Reasons must not defer work (`later`, `follow-up`, `followup`, `TBD`,
  `todo`). Add evidence first and the label last; keep internal status out of the release-note body.
  The `ai-corpus / verify` gate enforces the decision using live state. Comment edits do not trigger it:
  re-run the failed check with `gh run rerun <run-id> --failed --repo Cratis/Screenplay` after an edit.
- Diagnostic codes are permanent: never reuse or renumber one.
- Samples follow `.cratis/ai/rules/project/samples.md`; regenerated vectors are reviewed, never hand-edited.
