// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

/// <summary>
/// Lists the environment values a model needs, so an environment can be completed before the application runs.
/// </summary>
static class EnvironmentCommand
{
    static readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        string? target = null;
        var format = "text";
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == CliCommandCatalog.Format.Name)
            {
                if (++index >= arguments.Length || arguments[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine($"{argument} requires a value.");
                    return 2;
                }

                format = arguments[index];
            }
            else if (target is null && !argument.StartsWith("--", StringComparison.Ordinal))
            {
                target = argument;
            }
            else
            {
                error.WriteLine($"Unexpected argument '{argument}'. Usage: {CliCommandCatalog.EnvironmentKeys.Usage}");
                return 2;
            }
        }

        if (format is not ("text" or "json"))
        {
            error.WriteLine("--format must be text or json.");
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
            if (snapshot.Sources.Count == 0)
            {
                error.WriteLine($"No .play files found beneath '{target}'.");
                return 2;
            }

            if (!snapshot.Compilation.Success)
            {
                error.WriteLine("Listing the environment keys requires an error-free source model.");
                foreach (var diagnostic in snapshot.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error))
                {
                    error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                }

                return 1;
            }

            Write(EnvironmentRequirements.For(snapshot.Compilation.Value!), format, output);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Could not read '{target}': {exception.Message}");
            return 2;
        }
    }

    static void Write(EnvironmentRequirements requirements, string format, TextWriter output)
    {
        if (format == "json")
        {
            var keys = requirements.Keys.Select(key => new
            {
                key = key.Key,
                secret = key.IsSecret,
                source = key.Source.ToString(),
                owner = key.Owner,
                description = key.Description
            });
            output.WriteLine(JsonSerializer.Serialize(new { keys }, _json));
            return;
        }

        if (requirements.Keys.IsEmpty)
        {
            output.WriteLine("The model needs nothing from its environment.");
            return;
        }

        foreach (var key in requirements.Keys)
        {
            output.WriteLine(key.IsSecret ? $"{key.Key}  (secret)" : key.Key);
        }
    }
}
