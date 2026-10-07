// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_source_only_tree_is_deeply_nested : given.a_timeline
{
    [Fact]
    void should_prove_a_timeline_permutation_beyond_the_default_json_depth()
    {
        var before = Nested(false);
        var after = Nested(true);
        before.Compilation.Value.ShouldBeNull();
        after.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineRepairs.Timeline(before).SourceValid.ShouldBeTrue();
        WorkspaceTimelineRepairs.Timeline(after).SourceValid.ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeTrue();
    }

    [Fact]
    void should_discover_and_propose_the_deep_source_only_repair()
    {
        var workspace = Nested(false);
        workspace.Compilation.Value.ShouldBeNull();
        var repair = Repair(workspace, "E");
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }

    static ScreenplayWorkspace Nested(bool reversed)
    {
        var source = "module M\n";
        for (var depth = 0; depth < 36; depth++) source += new string(' ', (depth + 1) * 2) + $"feature F{depth}\n";
        var consumer = Slice("Consumer", [], "E").Replace("ConsumerView optional", "ConsumerView[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var producer = Slice("Producer", ["E"]);
        source += Indent(reversed ? producer + consumer : consumer + producer, 74);
        var document = WorkspaceDocument.Create("root.play", PortablePlayPath.Parse("root.play"), Encoding.UTF8.GetBytes(source));
        var catalog = SemanticIdentityCatalog.Create(ApplicationIdentity.Create("Timeline"), [new(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)], [], []);

        return ScreenplayWorkspace.Create("Timeline", [document], catalog);
    }
}
