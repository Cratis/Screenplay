// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_editor_eligibility_is_shared : given.a_command_production
{
    readonly List<string> _mismatches = [];
    int _vectors;
    int _offers;
    int _refusals;

    void Because()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(VectorPath()));
        foreach (var vector in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            _vectors++;
            var name = vector.GetProperty("name").GetString();
            Create(string.Join('\n', vector.GetProperty("source").EnumerateArray().Select(line => line.GetString())));
            var index = WorkspaceSyntaxIndex.Create(Workspace);
            var diagnostics = index.RepairableDiagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ToDictionary(diagnostic => diagnostic.Location.Line);
            foreach (var line in vector.GetProperty("eligibleLines").EnumerateArray().Select(line => line.GetInt32()))
            {
                _offers++;
                if (!diagnostics.TryGetValue(line, out var diagnostic) || WorkspaceDiagnosticRepairs.Find(index, Workspace.Revision, diagnostic).IsEmpty)
                {
                    _mismatches.Add($"{name}:{line}: the editor offers a repair C# refuses");
                }
            }
            foreach (var line in vector.GetProperty("refusedLines").EnumerateArray().Select(line => line.GetInt32()))
            {
                _refusals++;
                if (!diagnostics.TryGetValue(line, out var diagnostic) || !WorkspaceDiagnosticRepairs.Find(index, Workspace.Revision, diagnostic).IsEmpty)
                {
                    _mismatches.Add($"{name}:{line}: expected a reported diagnostic with no safe C# repair");
                }
            }
        }
    }

    [Fact] void should_check_the_shared_vectors() => _vectors.ShouldBeGreaterThan(16);
    [Fact] void should_check_offered_repairs() => _offers.ShouldBeGreaterThan(4);
    [Fact] void should_check_refused_repairs() => _refusals.ShouldBeGreaterThan(5);
    [Fact] void should_never_offer_a_repair_the_binder_refuses() => string.Join(Environment.NewLine, _mismatches).ShouldEqual(string.Empty);

    static string VectorPath([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "production-quick-fixes.json");
    }
}
