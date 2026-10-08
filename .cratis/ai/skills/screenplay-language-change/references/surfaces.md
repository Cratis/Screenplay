# In-repository surfaces

One entry per surface: where it lives, what to change, how to verify. Paths are relative to the repository
root. C# specs sit beside the code in `for_*` folders and run with `dotnet test`; TypeScript specs run with
`yarn workspace <name> test` (vitest). Run the narrowest check while iterating, the whole gate at the end
(see section 6 of the skill).

The C# compiler project `Source/DotNET/Screenplay/Screenplay.csproj` holds most C# specs, so
`dotnet test Source/DotNET/Screenplay/Screenplay.csproj` runs the compiler-side surfaces 1 to 11 and 20 to 21.

## Regeneration variables

Each one rewrites a checked-in file, then fails on purpose so a regenerating run can never pass in CI.
Review the diff, then rerun without the variable.

| Variable | Rewrites | Command |
| --- | --- | --- |
| `SCREENPLAY_REGENERATE_TRANSPORT_SCHEMA=1` | `Source/Screenplay/Compiler/Syntax/SyntaxDefinitions.ts` | `dotnet test Source/DotNET/Screenplay/Screenplay.csproj` |
| `SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS=1` | `Source/Screenplay/Compiler/Syntax/SyntaxCollections.ts` | same |
| `SCREENPLAY_REGENERATE_GOLDEN=1\|3\|4\|5\|6\|7` | ESM golden vectors in `Source/DotNET/Screenplay/Semantics/Serialization/Golden/` (`1` all, a digit one version; use `7` for v7 changes) | `dotnet test Source/DotNET/Screenplay/Screenplay.csproj -c Debug` |
| `SCREENPLAY_REGENERATE_EVENT_CONTEXT=1` | `Source/Screenplay/Monaco/screenplay-language/event-context-catalog.ts` and the generated table in the documentation | same |
| `SCREENPLAY_REGENERATE_EVENT_CONTEXT_VECTORS=1` | `Source/Screenplay/Monaco/screenplay-language/event-context-resolution-vectors.json` | same |
| `SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1` | Golden syntax in `Source/Screenplay/Compiler/Conformance/*.syntax.json` | `yarn workspace @cratis/screenplay-compiler test` |

## C# compiler (`Source/DotNET/Screenplay`)

### 1. Syntax tree and walker

- Paths: `Syntax/*.cs` (one `*Syntax.cs` per node), `Syntax/Specifications/`, `Syntax/Projections/`,
  `Syntax/Captures/`, `Syntax/ScreenplaySyntaxWalker*.cs`, `Syntax/Visitors.cs`, `Syntax/DirectiveLocationKeys.cs`.
- Change: add or alter the node record, visit it in the walker part for its area, register directive
  locations when the construct carries source directives.
- Verify: `Syntax/for_ScreenplaySyntaxWalker`, per-node specs; the AST JSON schema specs (surface 3).

### 2. Parser and parse-time validators

- Paths: `Parsing/ScreenplayParser.cs` and `Parsing/SliceParser.cs` dispatch on the keyword; one
  `*Parser.cs` per construct; `*Validator.cs` for parse-time checks; `ExpressionParser`,
  `StructuredValueParser`, `SourceLineSplitter`.
- Change: recognize the keyword and line grammar; add validators for what the grammar alone cannot reject.
- Verify: `Parsing/for_*` specs and `for_ScreenplayCompiler`. The spec
  `for_LanguageService/when_comparing_keywords_against_the_parsers` derives the keyword set from the
  `case` labels in the two dispatch parsers and fails when the Monaco keyword lists lack one (surface 18).
  `for_Documentation/when_comparing_the_grammar_against_the_parsers` fails when `grammar.md` and the
  reference page lack the production (surface 20).

### 3. AST JSON schema (`syntax-schema`)

- Paths: `Syntax/Serialization/` (`SyntaxSchema.cs`, `SyntaxSchemaWriter.cs`, `SyntaxJson*.cs`).
- Change: a new node kind or member appears in the schema automatically; the checked-in TypeScript mirrors
  must follow.
- Verify: `Syntax/Serialization/for_SyntaxSchema`, `for_SyntaxJson`. When the transport schema or collection
  contract specs fail, regenerate with `SCREENPLAY_REGENERATE_TRANSPORT_SCHEMA=1` and
  `SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS=1`. Explain the AST contract in
  `Documentation/screenplay/ast-compatibility.md` and `ast-authoring.md` when members change.

### 4. Diagnostic codes

- Paths: `Diagnostics/DiagnosticCodes.cs` (the registry), the TypeScript mirror
  `Source/Screenplay/Compiler/Diagnostics/DiagnosticCodes.ts`, the Monaco list
  `Source/Screenplay/Monaco/screenplay-language/diagnostic-codes.ts`, and
  `Documentation/screenplay/diagnostics.md`.
- Change: add the code to all three lists and to the documentation page. Never reuse or renumber a code.
  A retired code stays, marked as such.
- Verify: `Diagnostics/for_DiagnosticCodes` (includes
  `when_holding_the_catalogue_against_the_documentation`). Cases the TypeScript compiler must also report go
  in `Source/Screenplay/Compiler/Conformance/diagnostics.json` (surface 17).

### 5. Binder, semantic validators, ESM admission

- Paths: `Languages/ScreenplayLanguageRegistry.cs` (built-in triggers and inline-code languages; keep aligned
  with editors and downstream consumers), `Semantics/SemanticModelBinder.*.cs`, `SemanticModelValidator.*.cs`, `SemanticValueValidator.cs`,
  `SemanticModelCompiler.cs`.
- Change: bind the construct into the semantic model, or give it an explicit disposition in the binder
  (leaving it unbound does not by itself produce `PLAY0268`; the binder enumerates dispositions). Add a
  specification that pins the admission or refusal. Distinguish unsupported behavior (`PLAY0268`) from
  deferred or report-only syntax (`PLAY0269`/`PLAY0270`). Say which in the documentation page and in the MCP descriptions (surface 13).
- Verify: `Semantics/for_SemanticModelBinder`, `for_SemanticModelCompiler`, `for_SemanticCompilation`,
  `for_Compatibility`.

### 6. Executable semantic model versions and golden vectors

- Paths: `Semantics/Versions.cs` (language and semantic versions), `Semantics/ExecutableSemanticModel*.cs`,
  `Semantics/Serialization/`, goldens in `Semantics/Serialization/Golden/` (read its `README.md`).
- Change: allocate or reuse an ESM version per `decisions/0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md`;
  change the source model for the goldens first, then regenerate with `SCREENPLAY_REGENERATE_GOLDEN`.
- Verify: `Semantics/for_SemanticVersions`, `for_ExecutableSemanticModel`, and the vectors project
  (surface 16). A serialization change can change the bytes in `Screenplay.CanonicalCorpus`, which the golden
  mechanism does not rewrite; Stage compares against that corpus, so the two packages are released together.

### 7. Reference evaluator and specification runner

- Paths: `Semantics/Execution/` (`SemanticEvaluator.cs`, `SemanticExecutionPlan.cs`, the specification
  runner, `SemanticClock.cs`).
- Change: give the construct runtime meaning, or state it is not executable.
- Verify: `Semantics/Execution/for_SemanticSpecificationRunner` and the other specs under `Semantics/Execution`.

### 8. Canonical printer and exact-source round trip

- Paths: `Printing/ScreenplayPrinter*.cs` (one part per area), `ScreenplaySyntaxText*.cs`,
  `ScreenplayWriter.cs`, `UnsupportedSyntaxForPrinting.cs`.
- Change: print the node in canonical form; keep comments and trivia. A node the printer cannot print
  must throw `UnsupportedSyntaxForPrinting` rather than drop text.
- Verify: `for_ScreenplayPrinter`, `for_ScreenplayWriter`, `Printing/for_ScreenplaySyntaxText`.

### 9. Folder merge, imports and layout

- Paths: `Files/PlayFolderMerge*.cs`, `Files/PlayImports.cs`, `Files/PlayGlob.cs`, `Files/PlayPlacement.cs`,
  `Files/AuthoredOrder.cs`, `Workspaces/WorkspaceFolderLayout.cs`, `Workspaces/WorkspaceReferenceLayout.cs`,
  and in the MCP server `Source/DotNET/Screenplay.Mcp/McpLayout*.cs`.
- Change: where the construct may sit, how folder merge combines it, what `recommend-layout` and
  `expand-layout` suggest.
- Verify: `Files/for_PlayFolderMerge`, `for_PlayImports`, `for_PlayFiles`, `for_AuthoredOrder`, and the layout specs under
  `Source/DotNET/Screenplay.Mcp`.

### 10. Repairs

- Paths: `Workspaces/Workspace*Repairs.cs`, `WorkspaceDiagnosticRepairs.cs`, `WorkspaceRepairVerification.cs`;
  MCP `McpRepair*`; VS Code `Source/Screenplay/VSCodeExtension/Repair*.ts`;
  `Documentation/screenplay/mcp/repair-capabilities-v1.schema.json`.
- Change: a new or changed diagnostic may need a typed repair proposal (decision 0014). When a code is
  intentionally not repairable, the capability list and the documentation must say so.
- Verify: `Workspaces/for_WorkspaceRepairVerification`, `for_WorkspaceRefactoring`, MCP repair specs, and
  `yarn workspace screenplay test` (`for_Repair*`).

### 11. Rename and refactoring

- Paths: `Workspaces/WorkspaceRefactoring.cs`, `WorkspaceEventRefactorings.cs`, `WorkspaceRenameRequest.cs`,
  `WorkspaceAuthoring*.cs`, `WorkspaceReference*.cs`.
- Change: a new declaration or reference kind needs rename and find-references support, or an explicit
  refusal that the documentation states.
- Verify: `Workspaces/for_WorkspaceRefactoring`, `for_WorkspaceAuthoring`, `for_ScreenplayWorkspace`.

## TypeScript workspaces

### 12. Dependency graph and event model board

- C# paths: `Source/DotNET/Screenplay/Dependencies/` (`SliceReferences.cs`, `DependencyGraph.cs`,
  `DeclaredDependencies.cs`, `DeclaredDependencyTargets.cs`) drive C# diagnostics and MCP dependency results
  independently of the TypeScript graph. A new construct that references a slice, event or read model needs
  its reference classification updated there, with specs in `for_SliceReferences`, `for_DependencyGraph`,
  `for_DeclaredDependencies`, `for_DeclaredDependencyTargets`.

- Paths: `Source/Screenplay/EventModels/` (`Mapping/to*.ts`, `Mapping/EventModelDocumentVisitor.ts`,
  `Dependencies/`, `Schemas/`, `Document/`), `Source/Screenplay/Compiler/Dependencies/`,
  `Source/Screenplay/McpApp/` (`compileBoards.ts`, `VisualizedModel.ts`), the board in
  `Source/Screenplay/VSCodeExtension/EventModelBoard/`, and `Source/Screenplay/Views` for shared views.
- Change: map the construct to the board document and visuals, or show in a spec why it has no board form.
- Verify: `yarn workspace @cratis/screenplay-event-models test`, `@cratis/screenplay-mcp-app test`,
  `@cratis/screenplay-views test`. `EventModels/Mapping/for_EventModelDocumentVisitor/when_mapping_the_samples`
  maps every sample and fails when a screen falls to the generic user instead of a persona.

### 17. TypeScript compiler and conformance vectors

- Paths: `Source/Screenplay/Compiler/` (`Parsing/*Parser.ts`, `Syntax/`, `Diagnostics/`, `Authoring/`),
  `Source/Screenplay/Compiler/Conformance/` (`manifest.json`, `constructs.play`, `*.syntax.json`,
  `diagnostics.json`).
- Change: read the construct like the C# parser does. Add it to `Conformance/constructs.play` or add a
  document to `manifest.json`; add invalid cases to `diagnostics.json`. The compiler may skip a construct,
  but then the C# compiler alone reports it and the document says so (`Documentation/screenplay/typescript-compiler.md`).
- Verify: `yarn workspace @cratis/screenplay-compiler test` (enforces a coverage threshold). Regenerate the
  golden syntax with `SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1 yarn workspace @cratis/screenplay-compiler test`,
  review, rerun. The C# side is held to the same files by
  `Syntax/Serialization/for_SyntaxJson/when_holding_the_typescript_compiler_to_it` and
  `..._to_its_diagnostics`.

### 18. Monaco language service

- Paths: `Source/Screenplay/Monaco/screenplay-language/`: `tokens.ts` and `language.ts` (highlighting),
  `keyword-docs.ts` and `hover-content.ts` (hover), `completion-items.ts`, `completion-planner.ts`
  (completion), `validation.ts`, `diagnostic-codes.ts`, `sub-languages/` for PDL and CDL,
  `event-context-catalog.ts` (generated). The editor app is `Source/Screenplay/Monaco/screenplay-editor`.
- Change: highlight, document, complete and validate the new form. The bundled sample
  `screenplay-editor/samples/invoicing.play` is a copy of `Samples/Invoicing/invoicing.play`: update both
  together (it is a document in the conformance manifest).
- Verify: `yarn workspace @cratis/screenplay-language test`; the keyword spec named under surface 2.

### 19. VS Code extension

- Paths: `Source/Screenplay/VSCodeExtension/syntaxes/screenplay.tmLanguage.json` (TextMate grammar),
  `language-configuration.json`, `package.json` (contributions), `Completions.ts`, `Hover.ts`,
  `Diagnostics.ts`, `CodeActions.ts`, `InlayHints.ts`, `Repair*.ts`, `EventModelBoard/` (native editor),
  `WorkspaceApplication.ts`. There is no snippets file.
- Change: add the keyword and form to the TextMate grammar; wire completion, hover, diagnostics and code
  actions; make the native editor and board show it.
- Verify: `yarn workspace screenplay test`. `for_ReactionGrammar` and `for_SpecificationGrammar` load the
  real grammar with `vscode-textmate`, so a highlighting spec for the new form belongs there.
  `Documentation/screenplay/vscode.md` describes the behavior.

## Server, tool and corpus

### 13. MCP server

- Paths: `Source/DotNET/Screenplay.Mcp/`: `McpToolCatalog.cs` (tool names and descriptions, including the
  `syntax-schema` and `PLAY0268` wording), `McpToolSchemas.cs` (arguments), `McpTools.cs`, `McpAstSchemas.cs`,
  `McpConnection.cs` (the `Instructions` string), `McpAuthoring*.cs`.
- Change: a new tool needs a catalog entry, an argument schema and a handler; a changed construct may need
  description wording changes; admission wording must match surface 5.
- Verify: `dotnet test Source/DotNET/Screenplay.Mcp` (`for_McpConnection`, `for_McpAuthoringWorkflow`; the
  transcript spec lists every tool name). Update `Documentation/screenplay/mcp/*.md`, especially
  `reference.md`, `authoring-tools.md` and `install.md` (it lists tool names).

### 14. CLI tool

- Paths: `Source/DotNET/Tool/Program.cs`, `Source/DotNET/Tool/Mcp/`, `Source/DotNET/Tool/README.md`,
  `Documentation/screenplay/tool.md`.
- Change: only when a command, flag or output changes.
- Verify: `dotnet test Source/DotNET/Tool`; `dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing`.

### 15. Contexts package

- Paths: `Source/DotNET/Screenplay.Contexts/` (`Contexts/*.cs`, `verify-package.py`).
- Change: only when the typed context or identity model changes.
- Verify: the C# specs; CI runs `python3 Source/DotNET/Screenplay.Contexts/verify-package.py ./Artifacts/NuGet 9999.0.0`
  after `dotnet pack`.

### 16. Canonical corpus and vectors

- Paths: `Source/DotNET/Screenplay.CanonicalCorpus/` (`Corpus/{InlineEvents,Reactions,ReadModelAbsence,RegisterProject}`),
  `Source/DotNET/Screenplay.CanonicalVectors.Specs/`.
- Change: add or extend a corpus document when the construct is something Stage, Studio or CritterStack
  conformance should exercise. The corpus is hand-authored, not regenerated.
- Verify: `dotnet test Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj`
  (CI runs it in Release on all supported runtimes).

## Documentation and records

### 20. Documentation

- Paths: `Documentation/screenplay/` (a page per construct, `grammar.md` as hand-maintained EBNF,
  `diagnostics.md`, `mcp/*.md`, `typescript-compiler.md`, `vscode.md`, `tool.md`, `toc.yml`).
- Change: update every page that describes the construct or code; add a new page to `toc.yml`. Fence
  examples as ```` ```screenplay ````.
- Verify: `Source/DotNET/Screenplay/for_Documentation` (compiles every example, checks links and navigation,
  holds the grammar to the parsers).

### 21. Samples

- Paths: `Samples/{Library,Invoicing,Commerce,TimeTracking}` and their `README.md`. The rules, including
  which sample covers what, are in `.cratis/ai/rules/project/samples.md`.
- Change: `Samples/Invoicing` covers every construct. Update it, and any sample where the form is natural.
- Verify: `for_Samples/when_compiling_the_samples`; `dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing`.
  `Library` and `Invoicing` are also conformance documents, so regenerate the syntax vectors after editing them.

### 22. Decision records

- Paths: `decisions/NNNN-*.md` and the index `decisions/README.md`.
- Change: write or supersede a record when the change settles meaning, scope, admission or an ESM version.
  Its "In scope" list names the surfaces the change reaches (see 0003 and 0006 for the pattern). Read the
  `cratis-engineering-decision-record` skill first.
- Verify: review; the index lists the record.

### 23. Spell lists

- Paths: `.vscode/settings.json` (`cSpell.words`) and `Source/Screenplay/VSCodeExtension/cspell-screenplay.json`
  (the words the extension registers with the spell checker so `.play` keywords are not flagged).
- Change: add new keywords and terms to both.
- Verify: none automated.
