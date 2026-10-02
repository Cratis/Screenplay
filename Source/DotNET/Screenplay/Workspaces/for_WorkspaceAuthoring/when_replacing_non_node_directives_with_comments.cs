// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_non_node_directives_with_comments : Specification
{
    const string Source = """
        policy Member
          require authenticated
        persona Clerk
          policy Member // policy note
          description "A clerk" // persona note
        module Shop
          feature Orders
            slice StateChange Place
              event OrderPlaced
                id String
              event OrderRemoved
              constraint UniqueOrder // header note
                message "Already placed" // message note
                released by OrderRemoved // release note
                ignore casing // casing note
                unique id on OrderPlaced // rule note
            slice StateView Board
              query List => Order[]
                scoped to identity // scope note
                by id String // by note
        """;

    ScreenplayWorkspace _workspace = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("order", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    [Fact]
    void should_carry_metadata_through_workspace_ast_replacements()
    {
        var entries = WorkspaceSyntaxIndex.Create(_workspace).Entries;
        var persona = entries.Single(entry => entry.Node is PersonaSyntax);
        var constraint = entries.Single(entry => entry.Node is ConstraintSyntax);
        var query = entries.Single(entry => entry.Node is QuerySyntax);
        var result = Propose(
            new ReplaceWorkspaceNode(persona.Handle, persona.Node, ((PersonaSyntax)persona.Node) with { Description = "An editor" }),
            new ReplaceWorkspaceNode(constraint.Handle, constraint.Node, ((ConstraintSyntax)constraint.Node) with { Message = "An existing order" }),
            new ReplaceWorkspaceNode(query.Handle, query.Node, ((QuerySyntax)query.Node) with { Scope = "global" }));

        result.Accepted.ShouldBeTrue();
        var printed = result.WritePlan!.Entries.Single().After!.Text;
        printed.ShouldContain("description \"An editor\" // persona note");
        printed.ShouldContain("constraint UniqueOrder // header note\n        unique id on OrderPlaced // rule note\n        released by OrderRemoved // release note\n        ignore casing // casing note\n        message \"An existing order\" // message note");
        printed.ShouldContain("by id String // by note\n        scoped to global // scope note");
        WorkspaceDroppedComments.In(result.WritePlan).ShouldBeEmpty();
        result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Message.ShouldContain("dropped no comments");
    }

    [Fact]
    void should_report_a_release_comment_if_the_release_is_removed()
    {
        var constraint = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is ConstraintSyntax);
        var result = Propose(new ReplaceWorkspaceNode(
            constraint.Handle,
            constraint.Node,
            ((ConstraintSyntax)constraint.Node) with { ReleasedBy = [] }));

        result.Accepted.ShouldBeTrue();
        WorkspaceDroppedComments.In(result.WritePlan!).Single().Text.ShouldEqual("// release note");
        result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Message.ShouldContain("dropped 1 comment");
        result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Code.ShouldEqual("PLAY0288");
    }

    WorkspaceAuthoringResult Propose(params WorkspaceAstOperation[] operations) => _workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Operations = [.. operations]
    });
}
