---
title: "Screenplay printing: generate .play text from syntax"
description: "Print a Screenplay syntax tree as .play text after an edit or model import, with normalized layout and comments retained where source metadata allows."
---

# Printing and generating

The Screenplay printer turns a syntax tree into `.play` text. Use it after changing a parsed model or building syntax from another representation, so your tool can produce a document that readers can review and compile again.

Printing normalizes layout rather than preserving every source byte. Parsed comments stay with their declarations where the source metadata allows; [authoring workspace](ast-authoring.md) `PreserveTrivia` is the option when unchanged bytes must survive. If your output is target application code rather than `.play` source, use the [rendering contract](interoperability.md#level-3-screenplay-to-artifact-rendering) instead.

## The printer

The printer ships in the `Cratis.Screenplay` package as `IScreenplayPrinter`, alongside the compiler:

```csharp
using Cratis.Screenplay.Printing;

var printer = new ScreenplayPrinter();
var source = printer.Print(application);
```

`Print` is overloaded for the whole document and for each standalone sub-language, mirroring the compiler's `Compile` methods:

| Method | Renders |
|---|---|
| `Print(ApplicationSyntax)` | a whole `.play` document |
| `Print(ProjectionSyntax)` | a standalone projection |
| `Print(SpecificationSyntax)` | a standalone specification |
| `Print(CaptureSyntax)` | a standalone capture |

The result uses two spaces per indentation level. Indentation defines block boundaries in Screenplay, so the printer supplies the layout the parser needs rather than leaving it to your exporter.

## Round-tripping

For a valid syntax tree, printing and compiling produces an equivalent syntax tree. Printing that result again gives identical text, which lets you check that generated source has reached a stable form:

```csharp
using Cratis.Screenplay;
using Cratis.Screenplay.Printing;

var compiler = new ScreenplayCompiler();
var printer = new ScreenplayPrinter();

var tree = compiler.Compile(source).Value!;
var printed = printer.Print(tree);

// printed compiles without diagnostics, and printing it again is identical
var reprinted = printer.Print(compiler.Compile(printed).Value!);
```

This is a syntax round trip, not a semantic validation result. An edit can still introduce an unresolved reference or change behavior. Check the resulting model with the semantic compiler when your tool needs that verdict, and review [what printing does not keep](#what-printing-does-not-keep) before replacing authored files.

Values supplied by your tool need the same care as parsed values. The printer escapes a `"` or a `\` in a description, message, label or tag using [the grammar's string escapes](grammar.md#string-escapes). Compiling the text restores the value; you do not need to strip quotes before printing it.

Numeric output is culture-invariant. Every numeric literal, whether `decimal`, `float`, `int`, `long` or `double`, uses a `.` separator regardless of `CurrentCulture`. A machine set to `nb-NO` therefore produces the same numeric spelling. Legacy `double` literals print as decimal text rather than exponent notation, preserving their represented double value on recompilation; exact-mode literals keep their exact canonical text.

The printer also writes the parentheses needed to preserve condition grouping. A [policy](policies.md) `require` or a `produces when` condition binds `and` tighter than `or`. Where they mix, explicit grouping lets the reader follow the tree without relying on remembered precedence.

## Key and route ordering

The printer retains trailing read-model `key` marks and writes query and reads `by` blocks in authored part order. A production prints `for`, then its `stream` and `streamId`, then tags and payload mappings. A reaction or reducer prints its `from` filter after description and documentation, before conditions, triggers or rules. These forms survive print/parse round trips.

## What printing does not keep

The printer is faithful to the syntax tree, and the tree does not hold everything
a file does:

- **Comments are kept, not their surrounding whitespace.** Leading comments stay with the
  declaration or member they annotate, trailing comments follow the printed line, and
  comments at the end of a block stay in that block. This includes comments before
  `populate`, `field`, `on submit`, and inline `on` behaviors inside a form, as well as
  comments above a form. Comments within screens and layouts stay with their anchored
  directives or slots. Template slot and behavior comments also stay with their nodes;
  comments before or on a template's `fits slot` line stay with that directive.
  Comments on declaration and screen file references and before contribution navigation
  also stay with their lines. The same applies to enum values and concept attribute reasons;
  policy requirements and persona policy references; theme compatibility and UI profile
  targets, packages, layout and theme; contribution labels and order; screen-action
  labels; and reducer, performer, constraint, reaction-trigger and handler file references.
  Capture keys, map headers, split targets and append conditions; projection sequences,
  automap settings, parent keys and child exclusions; specification caller fixtures,
  event sources, query arguments and event-order directives; and command validation,
  production targets and concurrency dimensions keep comments on their authored lines.
  Persona descriptions, constraint headers, unique rules, release events, casing and messages,
  and query descriptions, `by` parameters and `scoped to` declarations also keep their
  comments on the corresponding printed lines when canonical order differs from source order.
  Other descriptions, behavior order, reaction triggers and conditions, interaction
  conditions and action continuations keep their trailing comments on the authored
  line even when blank lines are inserted or declarations move.
  Adjacent trailing comments stay separate, including across a constraint's release and
  casing lines. An explicit default `severity error` on a requirement is omitted. Its trailing
  comment moves to the `require` line, unless a `message` follows: then it gets its
  own line inside the requirement body, before `message`. If `require` already has
  a trailing comment and no `message` follows, the severity comment gets its own
  line below `require`.
  When you reorder enum values or other scalar collection entries, comments follow
  unchanged values rather than old list positions. Renaming entries in place keeps
  their comments when the entry count and the order of other values stay the same.
  Otherwise, comments stay with unchanged values; comments on renamed or removed
  entries are dropped and reported in `dropped-comments`.
  When a single-valued directive is written more than once and the last one wins (for example
  a repeated `file`, query `by`, command `handler`, capture `source` or `key`, projection
  `parent`, table `on row-click`, or action `label` or `navigate`), comments on the replaced
  lines keep their own lines: directly above the retained line for `file`, `by`, `handler` and
  projection `parent`, and at the start of the declaration body otherwise. They are never merged
  into the declaration's header comment.
  When modules or features repeat an identical `authorize` gate across files, merging
  keeps one requirement and the duplicate-gate warning. Printing and collapsing the
  folder to a single document retain every gate's comments, including repeated text.
  When gates are merged across files, the comments on every `authorize` line of every
  gate, including trailing comments, become separate lines directly above the merged
  gate: in file-path order, then source order within each file.
  Repeated projection `automap` settings warn (`PLAY0452`):
  the last setting wins, but printing keeps each authored line and its comments until
  a typed edit changes the mode. Then only the edited setting prints: its comment
  stays inline, and comments on earlier settings appear on their own lines below it.
  If you remove the setting (inherit), those comments move to the start of the block
  body instead. Comment positions are source metadata, not members of the typed JSON
  syntax. Check
  `dropped-comments` before applying a layout proposal for comments that cannot be
  retained during canonical printing.
  Inside a form, the printer always writes `populate`, then fields, then `on submit`,
  then attached behaviors: a comment moves with the member it annotates when that
  canonical order differs from the authored order. The printer uses canonical two-space
  indentation. A tree created entirely from typed JSON has no authored comments to keep.
- **Order across files needs import evidence.** Parsed members of a slice, feature or
  module keep their authored order when they share a source file. MCP `expand-layout`
  also retains a child's position at its parent's import, so collapsing an import-based
  layout keeps contributions, templates and other members in their authored relative
  order. Without that evidence, a folder merge combines members whose line numbers
  cannot be compared, and the printer uses canonical kind order for that owner. A first-line
  declaration from another file follows the new-member insertion rule instead of displacing
  the owner's comparable members. Syntax created without source
  positions (including typed JSON) also uses canonical kind order. A new member added
  to a parsed owner prints after the last member of its kind, or before the first
  member of a later canonical kind when none exists. Features, slices and file imports
  inserted or moved by typed edits print before their next located sibling when one
  exists, so timeline moves and pins retain their requested position, including the first
  position of an imported file whose first declaration is on its first line. Workspace AST replacements
  inherit their original position, even though typed JSON omits source positions.
- **Blank lines are normalized.** The printer separates members with its own blank lines.

Round-tripping preserves comments and meaning, not blank lines or every authored
space. Use [authoring workspace](ast-authoring.md) `PreserveTrivia` when even the
unchanged source bytes must survive. Folder expansion keeps comments with their
declarations, including module header comments in the module file; each slice
file retains its within-slice authored order. Leading and trailing header comments
stay on the physical module, feature or slice declaration, including the first line
of an imported file; synthetic placement wrappers do not take ownership of them.
End comments also belong to the enclosing physical declaration rather than a placement wrapper;
comments with no enclosing physical declaration stay on the application.

## Generating from a model

An importer or model designer can start without source text. Build the syntax nodes directly, then print the application:

```csharp
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

var registered = new EventSyntax(
    "AccountRegistered",
    [new PropertySyntax("name", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
    SourceLocation.Start);

var slice = new SliceSyntax(
    SliceType.StateChange, "RegisterAccount",
    [registered], [], [], [], [], [], [], [], [],
    SourceLocation.Start);

var module = new ModuleSyntax(
    "Accounts", [],
    [new FeatureSyntax("Registration", [], [slice], SourceLocation.Start)],
    SourceLocation.Start);

var application = new ApplicationSyntax([], [], [], [module], SourceLocation.Start);

var source = new ScreenplayPrinter().Print(application);
```

Every node carries a `SourceLocation`. The printer uses comparable source positions
for member order; `SourceLocation.Start` is the placeholder for nodes constructed
without source text, which print in canonical kind order.

## When one document is too much

When a single generated document becomes hard to navigate, write the same application as a [folder of `.play` files](folders.md), with files organized by module, feature and slice. Folder compilation assembles them into one application; use that workflow rather than splitting printed text at arbitrary lines.

## See also

- [Compiler and CLI](tool.md) - the reverse direction, text to tree.
- [Visitors and traversal](visitors.md) - walking a tree instead of rendering it.
- [Folders](folders.md) - the same tree written as module, feature and slice folders.
- [Grammar](grammar.md) - the shape the printer produces.
- [Sub-language Pluggability](sub-languages.md) - how the language is layered.
