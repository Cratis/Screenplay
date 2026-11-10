// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool;

// Runs the library model comparison over two model versions and prints it as text or JSON.
static class DiffCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = CommandArguments.Parse(args, CliCommandCatalog.Diff);
        var format = arguments?.Option(CliCommandCatalog.Format) ?? "text";
        if (arguments is null || arguments.Positional.Count != 2 || format is not "text" and not "json")
        {
            error.WriteLine($"Usage: {CliCommandCatalog.Diff.Usage}");
            return 2;
        }

        foreach (var target in arguments.Positional)
        {
            if (ModelSource.Refuse(target) is { } refusal)
            {
                error.WriteLine(refusal);
                return 2;
            }
        }

        try
        {
            var before = Compared(ModelSource.Load(arguments.Positional[0]));
            var after = Compared(ModelSource.Load(arguments.Positional[1]));
            var difference = ModelComparison.Compare(before, after);
            var report = new DiffReport(Side(arguments.Positional[0], before), Side(arguments.Positional[1], after), difference);
            if (format == "json")
            {
                DiffJson.Write(report, output);
            }
            else
            {
                DiffText.Write(report, output);
            }

            return 0;
        }
        catch (IncompatibleModelIdentities exception)
        {
            error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception) when (ModelSource.IsReadFailure(exception))
        {
            error.WriteLine($"Could not compare: {exception.Message}");
            return 2;
        }
    }

    static ComparedModel Compared(ModelSourceLoad load) =>
        load.HasPersistedIdentities ? ComparedModel.WithIdentities(load.Workspace) : ComparedModel.WithoutIdentities(load.Workspace);

    static DiffSide Side(string path, ComparedModel model) => new(
        path,
        model.Workspace.Revision.ToString(),
        model.HasPersistedIdentities,
        model.Workspace.Compilation.Success ? model.Workspace.Compilation.Value?.Model?.Revision.ToString() : null);
}

// The report keeps the library difference whole so later summaries (per slice, for example) sit beside it.
sealed record DiffReport(DiffSide Before, DiffSide After, ModelDifference Difference);

sealed record DiffSide(string Path, string SourceRevision, bool PersistedIdentities, string? ModelRevision);
