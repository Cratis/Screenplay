// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_a_validate_block_with_duplicate_rules : Specification
{
    const string Header = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n";
    const string Tail = "      command D\n        label String\n";

    const string OrdinaryRules = "        validate // block A\n" +
        "          label rule Check // rule A\n" +
        "            file Same.cs // file A\n" +
        "          label rule Check // rule B\n" +
        "            file Same.cs // file B\n";

    const string PendingRules = "        validate // block A\n" +
        "          label rule Check // rule A\n" +
        "            implementation // wrapper A\n" +
        "              hint \"Same\" // hint A\n" +
        "          label rule Check // rule B\n" +
        "            implementation // wrapper B\n" +
        "              hint \"Same\" // hint B\n";

    const string PendingAndBare = "        validate // block A\n" +
        "          label rule Check // rule A\n" +
        "            implementation // wrapper A\n" +
        "              hint \"Same\" // hint A\n" +
        "          label rule Check // bare B\n" +
        "          label rule Check // bare C\n";

    [Fact] void should_keep_every_comment_for_duplicate_ordinary_rules() => Moved(OrdinaryRules).ShouldBeTrue();
    [Fact] void should_keep_every_comment_for_duplicate_pending_rules() => Moved(PendingRules).ShouldBeTrue();
    [Fact] void should_keep_every_comment_for_pending_and_bare_siblings() => Moved(PendingAndBare).ShouldBeTrue();

    static bool Moved(string block)
    {
        var workspace = ScreenplayWorkspace.Create("M", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Header + block + Tail))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("M")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => entry.Node is DeclarativeValidateSyntax);
        var parent = index.Entries.Single(entry => entry.Node is CommandSyntax { Name: "D" });
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new MoveWorkspaceNode(target.Handle, target.Node, parent.Handle, parent.Node, "validations")]
        });
        return result.Accepted && Comments(result.Workspace!).SequenceEqual(Comments(workspace));
    }

    static string[] Comments(ScreenplayWorkspace workspace) => [.. workspace.Documents.SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
        .Where(token => token.Kind == WorkspaceSourceTokenKind.Comment).Select(token => token.Text).Order(StringComparer.Ordinal)];
}
