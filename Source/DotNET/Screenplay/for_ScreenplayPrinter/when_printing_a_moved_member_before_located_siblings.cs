// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_moved_member_before_located_siblings : Specification
{
    [Fact]
    void should_move_a_later_feature_before_an_earlier_sibling()
    {
        var workspace = Create("module M\n  feature Consumer\n  feature Producer\n  feature Last\n");
        var result = MoveSecondBeforeFirst(workspace, "features");
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var features = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<FeatureSyntax>();
        features.Select(feature => feature.Name).ShouldEqual(["Producer", "Consumer", "Last"]);
    }

    [Fact]
    void should_preserve_the_output_of_an_already_accepted_identical_behavior_use_move()
    {
        var workspace = Create("behavior B\n  on click\n    notify info \"Hello\"\nmodule M\n  uses B // first occurrence\n  uses B // second occurrence\n  feature F\n");
        workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var result = MoveSecondBeforeFirst(workspace, "usedBehaviors");
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var text = result.WritePlan!.Entries.Single().After!.Text;
        text.IndexOf("// first occurrence", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("// second occurrence", StringComparison.Ordinal));
    }

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Timeline",
        [WorkspaceDocument.Create("root", PortablePlayPath.Parse("root.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Timeline")));

    static WorkspaceAuthoringResult MoveSecondBeforeFirst(ScreenplayWorkspace workspace, string member)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var siblings = index.Entries.Where(entry => entry.Member == member).ToArray();
        var producer = siblings[1];
        var parent = index.Find(producer.Parent!)!;

        return workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new MoveWorkspaceNode(producer.Handle, producer.Node, parent.Handle, parent.Node, member, 0)]
        });
    }
}
