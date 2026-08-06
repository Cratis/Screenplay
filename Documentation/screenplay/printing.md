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

Because the two directions agree, you can read a `.play` file, adjust the syntax tree - rename a slice, add an event, change a mapping - and print it back out without disturbing the rest of the document.

Two details make the guarantee hold for values you did not type yourself:

- **Strings are escaped.** A description, message, label or tag holding a `"` or a `\` prints with the backslash escapes described in [the grammar](grammar.md#string-escapes), and compiling that text gives the original value back. You never have to strip quotes out of a value before handing it to the printer.
- **Numbers are culture-invariant.** Every numeric literal - `decimal`, `float`, `int`, `long` or `double` - prints with a `.` decimal separator regardless of `CurrentCulture`, so output produced on a machine set to `nb-NO` compiles anywhere.
- **Grouping is written out.** A [policy condition](policies.md) combines strictly left to right, so the printer adds the parentheses a grouped condition needs to compile back to the same condition. You build the tree you mean and the text follows - there is no flag to remember to set.

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
    [registered], [], [], null, [], [], [], [], [],
    SourceLocation.Start);

var module = new ModuleSyntax(
    "Accounts", [],
    [new FeatureSyntax("Registration", [], [slice], SourceLocation.Start)],
    SourceLocation.Start);

var application = new ApplicationSyntax([], [], [], [module], SourceLocation.Start);

var source = new ScreenplayPrinter().Print(application);
```

Every node carries a `SourceLocation`. The printer ignores it, so `SourceLocation.Start` is a fine placeholder when you are constructing nodes rather than parsing them.

## When one document is too much

The printer renders the whole application as one document, which is what you want until the application outgrows a file anyone can navigate. At that point the same tree can be written as a folder instead - a folder per module, per feature and per slice - and compiled back as one application. Same printer underneath, same round-trip guarantee. See [Folders](folders.md).

## See also

- [Compiler and CLI](tool.md) - the reverse direction, text to tree.
- [Folders](folders.md) - the same tree written as module, feature and slice folders.
- [Grammar](grammar.md) - the shape the printer produces.
- [Sub-language Pluggability](sub-languages.md) - how the language is layered.
