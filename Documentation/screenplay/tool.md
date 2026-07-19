# Compiler and CLI

You have a folder of `.play` files and want to know they are valid before anything consumes them. The Screenplay compiler ships in two forms: a .NET library you can embed, and a command line tool that verifies every `.play` file in a directory tree and prints any problems in a readable, compiler style format.

## Install the CLI

The CLI is a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) published to NuGet as `Cratis.Screenplay.Tool`. Install it globally:

```bash
dotnet tool install -g Cratis.Screenplay.Tool
```

This puts a `screenplay` command on your path. Update it later with `dotnet tool update -g Cratis.Screenplay.Tool`.

## Verify your files

Run `screenplay` from the root of your project - it searches for every file matching the `**/*.play` glob pattern beneath the current directory, compiles each one and reports what it finds:

```bash
screenplay
```

You can also point it at a specific directory:

```bash
screenplay path/to/screenplays
```

Each problem is reported with its file, line and column, the offending source line and a caret pointing at the exact location:

```text
nested/broken.play(3,5): error: Unknown slice type 'Wat' - expected StateChange, StateView, Automation or Translate
    3 |     slice Wat DoIt
      |     ^

2 file(s) compiled - 1 error(s), 0 warning(s)
```

The exit code is `0` when everything compiles without errors and `1` otherwise, so the command slots straight into CI pipelines. Colors are enabled automatically on interactive terminals; disable them with `--no-color` or by setting the `NO_COLOR` environment variable.

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

To turn the syntax tree into your own representation, implement one or more of the visitor interfaces (`IApplicationSyntaxVisitor<T>`, `ISliceSyntaxVisitor<T>`, `IProjectionSyntaxVisitor<T>`, ...) and pass the root visitor to `Compile` - the compiler drives it once parsing succeeds. See [Sub-language Pluggability](sub-languages.md) for how the language itself is layered.

Discovering and compiling every `.play` file beneath a directory - what the CLI does - is a single call:

```csharp
using Cratis.Screenplay.Files;

var compilations = new PlayFileCompiler().CompileIn(rootDirectory);
```
