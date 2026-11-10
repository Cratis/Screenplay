// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Agents;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Ui;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

static class ExportCommand
{
    static readonly string[] _formats = ["esm", "openapi", "ui", "capability-manifest"];

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = CommandArguments.Parse(args, CliCommandCatalog.Export);
        var exitCode = Export(arguments, output, error);
        if (exitCode != 0 && arguments?.Option(CliCommandCatalog.ExportFormat) == "capability-manifest" && arguments.Option(CliCommandCatalog.Output) is { } path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    error.WriteLine($"Removed stale capability manifest '{path}'.");
                }
            }
            catch (Exception exception) when (ModelSource.IsReadFailure(exception))
            {
                error.WriteLine($"Could not remove stale capability manifest '{path}': {exception.Message}");
                return 2;
            }
        }

        return exitCode;
    }

    static int Export(CommandArguments? arguments, TextWriter output, TextWriter error)
    {
        var format = arguments?.Option(CliCommandCatalog.ExportFormat) ?? "esm";
        if (arguments is null || arguments.Positional.Count > 1 || !_formats.Contains(format, StringComparer.Ordinal) ||
            (arguments.Flag(CliCommandCatalog.Screens) && format != "esm") ||
            (format == "capability-manifest" && arguments.Option(CliCommandCatalog.Scope) is not null))
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
            var scope = arguments.Option(CliCommandCatalog.Scope);
            if (format == "capability-manifest") return ExportManifest(workspace, target, arguments.Option(CliCommandCatalog.Output), output, error);
            if (format == "ui") return ExportUi(workspace, target, scope, arguments.Option(CliCommandCatalog.Output), output, error);

            var model = workspace.Compilation.Success ? workspace.Compilation.Value?.Model : null;
            if (model is null)
            {
                var errors = workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
                error.WriteLine($"'{target}' has no executable model: {errors.Length} error(s).");
                foreach (var diagnostic in errors) error.WriteLine($"  {diagnostic.Code}: {diagnostic.Message}");
                return 1;
            }

            if (arguments.Flag(CliCommandCatalog.Screens))
            {
                var admitted = ExecutableScreens.Admit(workspace);
                if (!admitted.Success)
                {
                    error.WriteLine($"'{target}' has screens the executable model cannot hold: {admitted.Refusals.Length} refusal(s).");
                    foreach (var reason in admitted.Refusals) error.WriteLine($"  {reason}");
                    return 1;
                }

                model = admitted.Compilation!.Model;
            }

            if (scope is not null) model = ExecutableSemanticModelScope.Select(model, scope);
            var bytes = format == "openapi" ? OpenApiDocument.Write(ApiDescription.For(model)) : SemanticModelSerializer.Serialize(model);
            Write(bytes, arguments.Option(CliCommandCatalog.Output), output);
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

    static int ExportManifest(ScreenplayWorkspace workspace, string target, string? path, TextWriter output, TextWriter error)
    {
        var emission = AgentCapabilityManifest.From(workspace.Compilation);
        if (emission.Manifest is not { } manifest)
        {
            var errors = workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
            error.WriteLine(emission.Refusal switch
            {
                AgentCapabilityManifestRefusal.NoAgentsBlock => $"'{target}' declares no agents block; nothing is exposed and no manifest is emitted.",
                AgentCapabilityManifestRefusal.DescriptionTooLong => $"Operation '{emission.Operation}' exceeds the {AgentCapabilityManifest.MaximumDescriptionLength} Unicode code point description limit; no capability manifest is emitted.",
                _ => $"'{target}' has {errors.Length} error(s); no capability manifest is emitted."
            });
            foreach (var diagnostic in errors) error.WriteLine($"  {diagnostic.Code}: {diagnostic.Message}");
            return 1;
        }

        WriteManifest(AgentCapabilityManifestJson.Write(manifest), path, output);
        return 0;
    }

    static void WriteManifest(byte[] bytes, string? path, TextWriter output)
    {
        if (path is null)
        {
            output.Write(Encoding.UTF8.GetString(bytes));
            return;
        }

        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) return;
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    // Screens need only the sources; identities and routes are added when the model also has an executable model.
    static int ExportUi(ScreenplayWorkspace workspace, string target, string? scope, string? path, TextWriter output, TextWriter error)
    {
        var contract = UiContractBuilder.For(workspace);
        if (scope is not null) contract = UiContractBuilder.Select(contract, scope);
        if (contract.ModelRevision is null)
        {
            error.WriteLine($"'{target}' has no executable model ({workspace.Compilation.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)} error(s)); the UI contract carries no identities or routes.");
        }

        Write(UiContractJson.Write(contract), path, output);
        return 0;
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
