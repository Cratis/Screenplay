// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_glob_blocks_cannot_move_safely : given.a_timeline
{
    static ScreenplayWorkspace Model() => Create(
        ("root.play", "module M\n  feature F\n    import \"left/*.play\" // left files\n    import \"right/*.play\" // right files\n"),
        ("left/a.play", Slice("First", ["X"])),
        ("left/b.play", Slice("Consumer", [], "E")),
        ("right/y.play", Slice("Producer", ["E"], "X")),
        ("right/z.play", "// no timeline declaration\n"));

    [Fact]
    void should_replace_the_globs_with_every_matched_document_in_verified_order()
    {
        var workspace = Model();
        var repair = Repair(workspace, "E");
        var result = Propose(workspace, repair);
        result.Accepted.ShouldBeTrue();
        var feature = (FeatureSyntax)((ReplaceWorkspaceNode)repair.Operations.Single()).Node;
        feature.FileImports.Select(import => import.Pattern).ShouldEqual(["left/a.play", "right/y.play", "left/b.play", "right/z.play"]);
        var text = result.Workspace!.Documents.Single(document => document.Path.Value == "root.play").Text;
        text.Contains("*.play", StringComparison.Ordinal).ShouldBeFalse();
        text.ShouldContain("// left files");
        text.ShouldContain("// right files");
        result.Workspace.Documents.Select(document => document.Path).ShouldEqual(workspace.Documents.Select(document => document.Path));
        result.Workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
        Preserves(workspace, repair);
    }

    [Fact]
    void should_refuse_an_expansion_that_loses_a_matched_document()
    {
        var workspace = Model();
        var repair = Repair(workspace, "E");
        var operation = (ReplaceWorkspaceNode)repair.Operations.Single();
        var feature = (FeatureSyntax)operation.Node;
        var changed = feature with { FileImports = feature.FileImports.Where(import => import.Pattern != "right/z.play").ToArray() };
        Propose(workspace, repair with { Operations = [operation with { Node = changed }] }).Accepted.ShouldBeFalse();
    }
}
