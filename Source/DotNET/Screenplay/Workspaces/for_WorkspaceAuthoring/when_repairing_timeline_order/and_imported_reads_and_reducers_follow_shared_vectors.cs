// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_imported_reads_and_reducers_follow_shared_vectors : given.a_timeline
{
    [Fact]
    void should_offer_verified_repairs_for_the_shared_authored_order_references()
    {
        var root = Dependencies.for_DependencyGraph.given.a_conformance_suite.Root();
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance", "authored-order.json")));
        var offered = 0;
        foreach (var vector in vectors.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (!vector.TryGetProperty("repairs", out var expected)) continue;
            var original = Create([.. vector.GetProperty("files").EnumerateObject().Select(file => (file.Name, file.Value.GetString()!))]);
            var catalog = SemanticIdentityCatalog.Create(
                ApplicationIdentity.Create("Timeline"),
                [.. original.Documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
                [],
                []);
            var workspace = ScreenplayWorkspace.Create("Timeline", original.Documents, catalog);
            var index = WorkspaceSyntaxIndex.Create(workspace);
            foreach (var reference in expected.EnumerateArray())
            {
                var diagnostic = workspace.Compilation.Diagnostics.Single(value => value.Code == DiagnosticCodes.EventFromLaterSlice &&
                    value.Location.Path == reference.GetProperty("path").GetString() && value.Location.Line == reference.GetProperty("line").GetInt32());
                var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
                index.Find(repair.Subject)!.Kind.ShouldEqual(reference.GetProperty("subject").GetString());
                Preserves(workspace, repair);
                var after = Propose(workspace, repair).Workspace!;
                after.Compilation.Diagnostics.Count(value => value.Code == DiagnosticCodes.EventFromLaterSlice).ShouldEqual(1);
                offered++;
            }
        }
        offered.ShouldEqual(2);
    }
}
