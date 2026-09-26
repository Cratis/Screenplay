# Printing and generating

The compiler turns `.play` text into a syntax tree. The printer does the reverse: it turns a syntax tree back into `.play` text. Together they let a tool read Screenplay, change it, and write it back out - or build a tree from scratch and generate a `.play` file from a model that was never text to begin with.

This is what a designer or exporter uses: assemble the syntax nodes that describe an application, hand them to the printer, and get valid Screenplay you can save, diff and feed to anything that consumes `.play` files.

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

The result is indentation-based Screenplay using two spaces per level - the same offside-rule layout the compiler expects.

## Round-tripping

Compiling and printing are inverses. Printing a tree and compiling the result gives back an equivalent tree, and printing that again gives back identical text:

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

Because the two directions agree, you can read a `.play` file, adjust the syntax tree - rename a slice, add an event, change a mapping - and print it back out with the rest of the document meaning exactly what it did. Comments from parsed source stay with their declarations; layout is normalized - see [what printing does not keep](#what-printing-does-not-keep).

Two details make the guarantee hold for values you did not type yourself:

- **Strings are escaped.** A description, message, label or tag holding a `"` or a `\` prints with the backslash escapes described in [the grammar](grammar.md#string-escapes), and compiling that text gives the original value back. You never have to strip quotes out of a value before handing it to the printer.
- **Numbers are culture-invariant.** Every numeric literal - `decimal`, `float`, `int`, `long` or `double` - prints with a `.` decimal separator regardless of `CurrentCulture`, so output produced on a machine set to `nb-NO` compiles anywhere.
- **Grouping is written out.** Every condition - a [policy](policies.md) `require`, a `produces when` - binds `and` tighter than `or`, so the printer adds the parentheses a condition needs to compile back to the tree it came from, and adds them again wherever `or` and `and` mix so the text does not rely on the reader knowing which binds tighter. You build the tree you mean and the text follows - there is no flag to remember to set.

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
  When you reorder, insert, or remove enum values or other scalar collection entries,
  comments follow unchanged values rather than old list positions; comments attached
  to removed values are dropped. Repeated projection `automap` settings warn (`PLAY0452`):
  the last setting wins, but printing keeps each authored line and its comments.
  These positions are source metadata, not members of the typed JSON syntax. Check
  `dropped-comments` before applying a layout proposal for comments that cannot be
  retained during canonical printing.
  Inside a form, the printer always writes `populate`, then fields, then `on submit`,
  then attached behaviors: a comment moves with the member it annotates when that
  canonical order differs from the authored order. The printer uses canonical two-space
  indentation. A tree created entirely from typed JSON has no authored comments to keep.
- **Order across files cannot be recovered.** Parsed members of a slice, feature or
  module keep their authored order when they share a source file. A folder merge may
  combine members from different files; their line numbers cannot be compared, so
  the printer uses canonical kind order for that owner. Syntax created without source
  positions (including typed JSON) also uses canonical kind order. A new member added
  to a parsed owner prints after the last member of its kind, or before the first
  member of a later canonical kind when none exists. Workspace AST replacements
  inherit their original position, even though typed JSON omits source positions.
- **Blank lines are normalized.** The printer separates members with its own blank lines.

Round-tripping preserves comments and meaning, not blank lines or every authored
space. Use [authoring workspace](ast-authoring.md) `PreserveTrivia` when even the
unchanged source bytes must survive. Folder expansion keeps comments with their
declarations, including module header comments in the module file; each slice
file retains its within-slice authored order.

## Generating from a model

You do not have to start from text. Build the syntax nodes directly and print them to generate Screenplay from your own representation:

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

The printer renders the whole application as one document, which is what you want until the application outgrows a file anyone can navigate. At that point the same tree can be written as a folder instead - a folder per module, per feature and per slice - and compiled back as one application. Same printer underneath, same round-trip guarantee. See [Folders](folders.md).

## See also

- [Compiler and CLI](tool.md) - the reverse direction, text to tree.
- [Visitors and traversal](visitors.md) - walking a tree instead of rendering it.
- [Folders](folders.md) - the same tree written as module, feature and slice folders.
- [Grammar](grammar.md) - the shape the printer produces.
- [Sub-language Pluggability](sub-languages.md) - how the language is layered.
