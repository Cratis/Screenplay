// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_a_glob_orders_the_slices : given.a_timeline
{
    [Fact]
    void should_pin_the_producers_file_before_the_glob()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\" // keep discovering\n"),
            ("a.play", Slice("Consumer", [], "E")),
            ("z.play", Slice("Producer", ["E"])));
        var repair = Repair(workspace, "E");
        ((FileImportSyntax)((AddWorkspaceNode)repair.Operations.Single()).Node).Pattern.ShouldEqual("z.play");
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Documents.Single(document => document.Path.Value == "root.play").Text.ShouldContain("import \"*.play\" // keep discovering");
    }

    [Fact]
    void should_pin_a_safe_prefix_when_pinning_only_the_producer_creates_a_finding()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\"\n"),
            ("a.play", Slice("Consumer", [], "E")),
            ("b.play", Slice("Middle", ["X"])),
            ("c.play", Slice("Producer", ["E"], "X")));
        var repair = Repair(workspace, "E");
        Preserves(workspace, repair);
        var text = Propose(workspace, repair).Workspace!.Documents.Single(document => document.Path.Value == "root.play").Text;
        text.ShouldContain("import \"b.play\"");
        text.ShouldContain("import \"c.play\"");
        text.ShouldContain("import \"*.play\"");
    }

    [Fact]
    void should_refuse_an_explicit_pin_containing_glob_metacharacters()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\"\n"),
            ("a.play", Slice("Consumer", [], "E")),
            ("z[one].play", Slice("Producer", ["E"])));
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, Finding(workspace, "E")).ShouldBeEmpty();
    }
}
