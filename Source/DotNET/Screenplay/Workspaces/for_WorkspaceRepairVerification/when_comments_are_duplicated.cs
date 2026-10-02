// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_comments_are_duplicated : Specification
{
    WorkspaceAuthoringResult _result;

    void Because()
    {
        const string source = "concept Name : String // original intent\n";
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, new ScreenplayCompiler().Parse(source + "concept Other : String // original intent\n").Value!)]
        });
        result.Conflicts.ShouldBeEmpty();
        WorkspaceDroppedComments.In(result.WritePlan!).ShouldBeEmpty();
        _result = WorkspaceRepairVerification.RequireComments(result);
    }

    [Fact] void should_refuse_even_when_no_comment_was_dropped() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_comment_preservation_failure() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.RepairWouldDropComments);
    [Fact] void should_not_expose_a_partial_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_not_expose_a_write_plan() => _result.WritePlan.ShouldBeNull();
}
