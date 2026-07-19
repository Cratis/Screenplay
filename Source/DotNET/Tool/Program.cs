// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

var root = args.FirstOrDefault(arg => !arg.StartsWith('-')) ?? Directory.GetCurrentDirectory();
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Directory '{root}' does not exist");
    return 1;
}

var useColors = !Console.IsOutputRedirected &&
                Environment.GetEnvironmentVariable("NO_COLOR") is null &&
                !args.Contains("--no-color");

var compilations = new PlayFileCompiler().CompileIn(root).ToArray();
if (compilations.Length == 0)
{
    Console.WriteLine($"No .play files found beneath {root}");
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

var summary = $"{compilations.Length} file(s) compiled - {errors} error(s), {warnings} warning(s)";
if (useColors)
{
    var color = "\e[32m";
    if (errors > 0)
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

return errors > 0 ? 1 : 0;
