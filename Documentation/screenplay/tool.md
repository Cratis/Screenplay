# Compiler and CLI

You have a folder of `.play` files and want to know they are valid before anything consumes them. The Screenplay compiler ships in two forms: a .NET library you can embed, and a command line tool that verifies every `.play` file in a directory tree and prints any problems in a readable, compiler style format.

## Install the CLI

The CLI is a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) published to NuGet as `Cratis.Screenplay.Tool`. Install it globally:

```bash
dotnet tool install -g Cratis.Screenplay.Tool
```

This puts a `screenplay` command on your path. Update it later with `dotnet tool update -g Cratis.Screenplay.Tool`.

## Verify your files

Run `screenplay` from the root of your project - it searches for every file matching the `**/*.play` glob pattern beneath the current directory and reports what it finds:

```bash
screenplay
```

You can also point it at a specific directory:

```bash
screenplay path/to/screenplays
```

A directory is verified as **one application**: the files are merged before anything is resolved, so an event declared in one file and produced in another resolves rather than looking missing. See [Folders](folders.md) for what that means and how the files fit together.

Or point it at a single file, which is what you want when a generator just produced one and you only care about that one. A single file is verified on its own, so a name it uses but does not declare is reported:

```bash
screenplay path/to/invoicing.play
```

Each problem is reported with its file, line and column, its severity and stable code, the offending source line and a caret pointing at the exact location:

```text
nested/broken.play(3,5): error PLAY0028: Unknown slice type 'Wat' - expected StateChange, StateView, Automation or Translate
    3 |     slice Wat DoIt
      |     ^

2 file(s) compiled - 1 error(s), 0 warning(s)
```

The code is the part that never changes - match on it rather than on the message, which gets reworded. Every code is listed in [Diagnostics](diagnostics.md).

The exit code is `0` when everything compiles without errors and `1` otherwise, so the command slots straight into CI pipelines.

Warnings do not fail the run by default. When a pipeline demands a spotless document - a generated one, say - add `--warnaserror` and a single warning is enough to exit `1`:

```bash
screenplay path/to/invoicing.play --warnaserror
```

| Option | Effect |
|---|---|
| `--warnaserror` | Warnings fail the run - exit code `1` even with zero errors |
| `--no-color` | Never colorize output |

Colors are enabled automatically on interactive terminals; disable them with `--no-color` or by setting the `NO_COLOR` environment variable.

## Use the compiler as a library

Everything the CLI does lives in the `Cratis.Screenplay` NuGet package - parsing, the syntax tree, diagnostics, file discovery and formatting:

```bash
dotnet add package Cratis.Screenplay
```

Compiling source text gives you a syntax tree and any diagnostics:

```csharp
using Cratis.Screenplay;

var compiler = new ScreenplayCompiler();
var result = compiler.Compile(source);

if (result.Success)
{
    var application = result.Value!;
}
```

To turn the syntax tree into your own representation, derive from `ScreenplaySyntaxWalker` and override the node kinds you care about - the base class walks everything else, so a construct you never asked about cannot break you. The root visitor interfaces (`IApplicationSyntaxVisitor<T>`, `IProjectionSyntaxVisitor<T>`, ...) are the alternative when your consumer is one transformation of the whole document; pass one to `Compile` and the compiler drives it once parsing succeeds. See [Visitors and traversal](visitors.md) for both, and [Sub-language Pluggability](sub-languages.md) for how the language itself is layered.

Compiling every `.play` file beneath a directory as one application - what the CLI does for a directory - is a single call:

```csharp
using Cratis.Screenplay.Files;

var compilation = new PlayFileCompiler().CompileFolder(rootDirectory);
var application = compilation.Result.Value!;
```

A single file is a single call too:

```csharp
var compilation = new PlayFileCompiler().CompileFile(path);
```

`CompileIn(rootDirectory)` is the third option: it compiles every discovered file as a document in its own right and hands back one result per file. Reach for it only when the files genuinely are separate documents that happen to share a directory - see [Folders](folders.md) for why a folder is normally one application.

To go the other way - turn a syntax tree back into `.play` text, or generate Screenplay from a model - see [Printing and generating](printing.md).
