// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Tool;

// Writes the canonical executable semantic model bytes, the same bytes SemanticModelSerializer produces.
static class ExportCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = CommandArguments.Parse(args, CliCommandCatalog.Export);
        if (arguments is null || arguments.Positional.Count > 1 || (arguments.Option(CliCommandCatalog.ExportFormat) ?? "esm") != "esm")
        {
            error.WriteLine($"Usage: {CliCommandCatalog.Export.Usage}");
            return 2;
        }

        var target = arguments.Positional.Count == 1 ? arguments.Positional[0] : Directory.GetCurrentDirectory();
        if (ModelSource.Refuse(target) is { } refusal)
        {
            error.WriteLine(refusal);
            return 2;
        }

        try
        {
            var workspace = ModelSource.Load(target).Workspace;
            var model = workspace.Compilation.Success ? workspace.Compilation.Value?.Model : null;
            if (model is null)
            {
                var errors = workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
                error.WriteLine($"'{target}' has no executable model: {errors.Length} error(s).");
                foreach (var diagnostic in errors) error.WriteLine($"  {diagnostic.Code}: {diagnostic.Message}");
                return 1;
            }

            if (arguments.Option(CliCommandCatalog.Scope) is { } scope)
            {
                model = ExecutableSemanticModelScope.Select(model, scope);
            }

            Write(SemanticModelSerializer.Serialize(model), arguments.Option(CliCommandCatalog.Output), output);
            return 0;
        }
        catch (InvalidSemanticModelScope exception)
        {
            error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception) when (ModelSource.IsReadFailure(exception))
        {
            error.WriteLine($"Could not export '{target}': {exception.Message}");
            return 2;
        }
    }

    // A file gets the exact canonical bytes; an identical file is left untouched for incremental builds.
    static void Write(byte[] bytes, string? path, TextWriter output)
    {
        if (path is null)
        {
            output.Write(Encoding.UTF8.GetString(bytes));
            return;
        }

        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) return;
        File.WriteAllBytes(path, bytes);
    }
}
