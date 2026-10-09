// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Processing;

namespace Cratis.Screenplay.Tool;

static class ProcessingReportCommand
{
    internal static int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        if (arguments.FirstOrDefault() != "processing")
        {
            error.WriteLine("Expected 'screenplay report processing [<file.play|folder>] --format json|markdown|csv'.");
            return 2;
        }

        string? target = null;
        string? controllerName = null;
        string? controllerContact = null;
        var format = "markdown";
        for (var index = 1; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == CliCommandCatalog.ProcessingFormat.Name || argument == CliCommandCatalog.ControllerName.Name || argument == CliCommandCatalog.ControllerContact.Name)
            {
                if (++index >= arguments.Length || arguments[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine($"{argument} requires a value.");
                    return 2;
                }

                if (argument == CliCommandCatalog.ProcessingFormat.Name)
                {
                    format = arguments[index];
                }
                else if (argument == CliCommandCatalog.ControllerName.Name)
                {
                    controllerName = arguments[index];
                }
                else
                {
                    controllerContact = arguments[index];
                }
            }
            else if (target is null && !argument.StartsWith("--", StringComparison.Ordinal))
            {
                target = argument;
            }
            else
            {
                error.WriteLine($"Unexpected argument '{argument}'.");
                return 2;
            }
        }

        if (format is not ("json" or "markdown" or "csv"))
        {
            error.WriteLine("--format must be json, markdown or csv.");
            return 2;
        }

        target ??= Directory.GetCurrentDirectory();
        if ((!File.Exists(target) && !Directory.Exists(target)) || (File.Exists(target) && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase)))
        {
            error.WriteLine($"'{target}' must be an existing .play file or model directory.");
            return 2;
        }

        try
        {
            var snapshot = McpSnapshot.Compile(target, File.Exists(target));
            if (!snapshot.Compilation.Success)
            {
                error.WriteLine("Processing record requires an error-free source model.");
                foreach (var diagnostic in snapshot.Compilation.Diagnostics) error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                return 1;
            }
            if (snapshot.Sources.Count == 0)
            {
                error.WriteLine($"No .play files found beneath '{target}'.");
                return 2;
            }

            ProcessingReportText.Write(ProcessingRecord.Create(snapshot.Compilation.Value!, controllerName, controllerContact), format, output);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Could not report '{target}': {exception.Message}");
            return 2;
        }
    }
}
