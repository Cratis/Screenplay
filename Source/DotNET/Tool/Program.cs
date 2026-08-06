// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

const string PlayExtension = ".play";

var target = args.FirstOrDefault(arg => !arg.StartsWith('-')) ?? Directory.GetCurrentDirectory();
var isFile = File.Exists(target);
if (!isFile && !Directory.Exists(target))
{
    Console.Error.WriteLine($"'{target}' does not exist");
    return 1;
}

if (isFile && !target.EndsWith(PlayExtension, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"'{target}' is not a {PlayExtension} file");
    return 1;
}

var useColors = !Console.IsOutputRedirected &&
                Environment.GetEnvironmentVariable("NO_COLOR") is null &&
                !args.Contains("--no-color");

var warnAsError = args.Contains("--warnaserror");

// A folder of .play files describes one application, so it is verified as one - a name declared in one file
// and used in another resolves, exactly as it would within a single document.
var playFileCompiler = new PlayFileCompiler();
var sources = new Dictionary<string, string>(StringComparer.Ordinal);
IEnumerable<Diagnostic> diagnostics;

if (isFile)
{
    var compilation = playFileCompiler.CompileFile(target);
    sources[compilation.File.RelativePath] = compilation.Source;
    diagnostics = compilation.Result.Diagnostics;
}
else
{
    var compilation = playFileCompiler.CompileFolder(target);
    foreach (var source in compilation.Sources)
    {
        sources[source.File.RelativePath] = source.Source;
    }

    diagnostics = compilation.Result.Diagnostics;
}

if (sources.Count == 0)
{
    Console.WriteLine($"No {PlayExtension} files found beneath {target}");
    return 0;
}

var fallbackFile = sources.Keys.First();
var formatter = new DiagnosticFormatter();
var errors = 0;
var warnings = 0;

foreach (var diagnostic in diagnostics
    .OrderBy(diagnostic => diagnostic.Location.Path ?? fallbackFile, StringComparer.Ordinal)
    .ThenBy(diagnostic => diagnostic.Location.Line)
    .ThenBy(diagnostic => diagnostic.Location.Column))
{
    switch (diagnostic.Severity)
    {
        case DiagnosticSeverity.Error:
            errors++;
            break;
        case DiagnosticSeverity.Warning:
            warnings++;
            break;
    }

    var file = diagnostic.Location.Path ?? fallbackFile;
    Console.WriteLine(formatter.Format(file, diagnostic, sources.GetValueOrDefault(file, string.Empty), useColors));
}

if (errors + warnings > 0)
{
    Console.WriteLine();
}

var failed = errors > 0 || (warnAsError && warnings > 0);
var summary = $"{sources.Count} file(s) compiled - {errors} error(s), {warnings} warning(s)";
if (useColors)
{
    var color = "\e[32m";
    if (failed)
    {
        color = "\e[31m";
    }
    else if (warnings > 0)
    {
        color = "\e[33m";
    }

    Console.WriteLine($"{color}{summary}\e[0m");
}
else
{
    Console.WriteLine(summary);
}

return failed ? 1 : 0;
