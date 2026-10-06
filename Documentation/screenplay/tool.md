# Compiler and CLI

You have a folder of `.play` files and want to know they are valid before anything consumes them. The Screenplay compiler ships in two forms: a .NET library you can embed, and a command line tool that verifies every `.play` file in a directory tree and prints any problems in a readable, compiler style format.

## Install the CLI

The CLI is a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) published to NuGet as `Cratis.Screenplay.Tool`. Install it globally:

```bash
dotnet tool install -g Cratis.Screenplay.Tool
```

This puts a `screenplay` command on your path. Update it later with `dotnet tool update -g Cratis.Screenplay.Tool`.

### Run it in Docker

The same CLI is published as the `cratis/screenplay` image, for machines without the .NET SDK and for CI. Mount the files you want to verify and pass the mounted path:

```bash
docker run --rm -v "$PWD/specifications:/work:ro" cratis/screenplay --warnaserror /work
```

The image also runs the [MCP server](mcp/index.md): `docker run -i --rm -v "$PWD/specifications:/model" cratis/screenplay mcp /model`.

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

Or point it at a single file. A file is the root of an application: it is verified together with every file its [imports](imports.md) bring in, and a file without imports is verified on its own, so a name it uses but does not declare is reported:

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

The exit code is `0` when the check is clean, `1` for model defects, and `2` when the check could not run (for example, an invalid path, argument or unknown scope).

Warnings do not fail the run by default. When a pipeline demands a spotless document - a generated one, say - add `--warnaserror` and a single warning is enough to exit `1`:

```bash
screenplay path/to/invoicing.play --warnaserror
```

| Option | Effect |
|---|---|
| `--scope <Module>[.<Feature>[.<Slice>]]` | Report the named scope and declarations that directly reference it |
| `--warnaserror` | Warnings in the reported set fail the run - exit code `1` even with zero errors |
| `--no-color` | Never colorize output |

Colors are enabled automatically on interactive terminals; disable them with `--no-color` or by setting the `NO_COLOR` environment variable.

## Check one part of the application

Keep the application root as the input and name the part you changed:

```bash
screenplay path/to/screenplays --scope Billing.Invoices.SendInvoice --warnaserror
```

`--scope` uses case-sensitive, dotted module, feature and slice addresses, as MCP navigation does. Descendants are included; a partial name does not match another module. Nested features use their full dotted address. An unknown scope is a usage error, not an empty successful check.

The compiler still resolves references across the whole application. Only diagnostics located in the selected declarations, plus declarations that directly reference them, are reported. A dependent declaration is included in full, not its whole slice; dependents of dependents are not included. The exit code and `--warnaserror` apply only to that reported set. The final summary labels its counts as **in scope**; a separate **Whole application** line always shows the full error and warning counts and how many diagnostics are outside the reported set. That line is red when the whole check fails, even if the scoped exit code is `0`.

Output includes the scope's declaration count, the additional direct-dependent declaration count, the diagnostic count, and **affected scopes** containing those dependents. `<application>` means an application-level declaration. Check the listed scopes or the whole root when you need wider assurance.

Impact uses the same explicit source-reference index as MCP `dependencies`. It does not inspect inline code, property paths, imports or expression identifiers, and ambiguous candidates are included conservatively. Unresolved references matching a name in scope and unresolved event consumers are included conservatively. **Possibly affected** counts other unresolved references outside the requested scope, including event consumers whose former target cannot be proved after a rename or removal. Diagnostics outside declaration ranges fall back to the file's import placement or its selected slice; diagnostics without a path or valid line are application-wide and included in every scoped result. A clean scoped check is not proof that the whole application is valid or executable. No watch or incremental cache is provided by this option.

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

`CompileFile` compiles that one document. `CompileApplication(path)` compiles the application the file is the root of - the file and everything its [imports](imports.md) bring in - which is what the CLI does:

```csharp
var compilation = new PlayFileCompiler().CompileApplication("application.play");
```

`CompileIn(rootDirectory)` is the third option: it compiles every discovered file as a document in its own right and hands back one result per file. Reach for it only when the files genuinely are separate documents that happen to share a directory - see [Folders](folders.md) for why a folder is normally one application.

To go the other way - turn a syntax tree back into `.play` text, or generate Screenplay from a model - see [Printing and generating](printing.md).
