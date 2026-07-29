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

var playFileCompiler = new PlayFileCompiler();
var compilations = isFile
    ? [playFileCompiler.CompileFile(target)]
    : playFileCompiler.CompileIn(target).ToArray();

if (compilations.Length == 0)
{
    Console.WriteLine($"No {PlayExtension} files found beneath {target}");
    return 0;
}

var formatter = new DiagnosticFormatter();
var errors = 0;
var warnings = 0;

foreach (var compilation in compilations)
{
    foreach (var diagnostic in compilation.Result.Diagnostics)
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

        Console.WriteLine(formatter.Format(compilation.File.RelativePath, diagnostic, compilation.Source, useColors));
    }
}

if (errors + warnings > 0)
{
    Console.WriteLine();
}

var failed = errors > 0 || (warnAsError && warnings > 0);
var summary = $"{compilations.Length} file(s) compiled - {errors} error(s), {warnings} warning(s)";
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
