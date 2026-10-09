---
applyTo: "**/*"
---

## Samples

`Samples/` at the repository root holds hand-written Screenplay applications, one per folder. They are the
living reference for the language, so they must say what the language says today.

| Sample | Shows |
| --- | --- |
| `Samples/Library` | The smallest useful application in one file - where a newcomer starts. |
| `Samples/Invoicing` | Every single-document, warning-free construct, with its `.strings` files - except the preview constructs listed below. |
| `Samples/Commerce` | Focused files composed with explicit imports at every level: a root that reads like a table of contents, a file per module and feature that imports its own folder, and one file per slice with nothing above it. |
| `Samples/TimeTracking` | Focused files composed from one root glob: module files declare their features inline and import each feature's step files, one slice per file. |

### Keep them current with the language

Any change to the language — grammar, a construct, a keyword, a diagnostic that starts firing, a deprecation, a
rename — updates the samples in the same pull request:

- **A new construct or form** is added to `Samples/Invoicing` and to any other sample where it is the natural
  way to say something. Add it the way an author would actually use it, not as an isolated fragment. If the binder
  refuses it only where it is used, it stays in Invoicing and is pinned in `when_binding_the_invoicing_sample`.
  If the binder refuses the whole model before binding (exact numeric mode, or anything
  `CommandProductionAdmission` refuses), it is a **preview construct**. List it under *Preview constructs* with
  its issue and a fixture that compiles with no diagnostics and binds to `PLAY0268` naming that issue. The pull
  request that admits it into an ESM version removes its row and adds it to Invoicing.
- **A changed or deprecated form** is rewritten in every sample that uses it. Samples never carry deprecated
  syntax (`PLAY0397`) or a form the compiler warns about.
- **A removed construct** is removed from every sample, along with any README text describing it.
- **A changed meaning** (what a construct does, not how it is written) is checked against each sample's
  specifications and comments, so the samples do not teach the old meaning.
- Update the sample's `README.md` when what the sample demonstrates changes.
- **The language lives in more than one place.** A change lands in the C# compiler, the TypeScript compiler
  (`Source/Screenplay/Compiler`, held to the C# one by the conformance documents in its `Conformance` folder), the
  Monaco language service (`Source/Screenplay/Monaco/screenplay-language`: highlighting, hover, completion,
  validation) and the VS Code extension's TextMate grammar (`Source/Screenplay/VSCodeExtension/syntaxes`). A
  sample that uses a new form is only correct once every one of them understands it.

### Preview constructs

These forms parse and print, but refuse the whole model before per-construct binding. Keep their fixtures
outside `Samples/` so Invoicing continues to exercise its individual binding dispositions.

| Construct | Issue | Fixture |
| --- | --- | --- |
| `numbers exact` | #285 | `Source/Screenplay/Compiler/Conformance/exact-named-rule-intent.play` |
| `eventsource`, `stream`, command routes | #302 | `Documentation/screenplay/fixtures/source-streams.play` |
| specification `stream`/`streamId`/`no stream` | #457 | `Source/Screenplay/Compiler/Conformance/specification-streams.play` |
| `system`, `operation`, operation specifications | #301 | `Documentation/screenplay/fixtures/operations.play` |
| refusals, redelivery, `then no events` | #433 | `Source/Screenplay/Compiler/Conformance/reaction-refusals-redelivery.play`, `Source/Screenplay/Compiler/Conformance/no-events.play` |

`for_Samples/when_holding_invoicing_to_the_language` reflects over concrete syntax nodes and requires every
kind missing from Invoicing to be classified exactly once as Preview, CoveredElsewhere (composition in a
multi-file sample or legacy syntax pinned by a warning spec, with a concrete, tested location), or Infrastructure
(error/trivia nodes only). `FileImportSyntax` is demonstrated by Commerce; `FileConstraintSyntax` stays in the
legacy compiler fixture because it warns with `PLAY0396`. Newly covered kinds must leave those lists. It checks
preview fixture compilation, node presence and issue-specific binding refusal, and holds this table to the
Preview list. Exact numeric mode is checked separately because it is a document option, not a syntax node.

### The conventions every sample keeps

- Every `StateChange` and `StateView` slice has a `screen`, and what gates it — module, feature and the slice's
  command or query `authorize` — is satisfied by the policies a `persona` holds, so the event model board draws
  the screen for that persona.
- Every slice has at least one `specification` written as Given/When/Then: `given caller` whenever the command or
  query is authorized, `when` a command or `when append` an event, and `then` the events, read models, queries,
  errors or denials that follow. Read-only views over state no event builds use `given readmodel`, `when query` and
  `then result`.
- At least one `StateView` slice in a non-trivial sample has a read model no event builds — a query with a
  `performer` — so the samples show that a view need not consume events.
- Every `$strings.<key>` a sample references is defined in every `.strings` file of that sample.
- Inline code always uses a tagged fence (```` ```csharp ````), never a language word on its own line.
- Enumeration values in specification steps and in `produces when` / `where` conditions are quoted
  (`status == "sent"`), so they read the same to the compiler and to the executable semantic model.
- Specifications use the form that matches what drives the slice: `when query` for a view no event builds,
  `when clock`, `when trigger` and `when capture` for automation and translate slices, and `given clock` when a
  value mapped from `$context.occurred` is asserted.
- A folder sample is made of focused files - one part of the story each, with no large file. When its root file
  imports the rest, every file in the folder is reachable from that root.

### Verify

`for_Samples/when_compiling_the_samples` compiles every folder under `Samples/` as one application and fails on any
error or warning, on a slice without a screen or specification, on an undefined string key, and on a file the
sample's root composite does not reach. It runs with
`dotnet test`. To check one sample from the command line:

```bash
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Invoicing
```

The TypeScript side holds the samples too. `Samples/Library` and `Samples/Invoicing` are documents in
`Source/Screenplay/Compiler/Conformance/manifest.json`, so changing either regenerates the golden syntax there
(`SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1`) and the C# compiler is held to it. The Monaco editor's bundled
`Source/Screenplay/Monaco/screenplay-editor/samples/invoicing.play` is a copy of `Samples/Invoicing/invoicing.play` -
update both together. The event model board's `when_mapping_the_samples` specification maps every sample and fails
when a screen falls to the generic user instead of a persona.

A new sample is a new folder under `Samples/` with a `README.md`; the specifications pick it up without changes.
