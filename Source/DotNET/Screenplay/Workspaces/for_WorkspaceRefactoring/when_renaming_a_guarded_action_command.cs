// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_guarded_action_command
{
    const string Source =
        """
        module Work
          feature Items
            slice StateView Details
              command Retry
              readmodel Item
                status String
              query Details => Item
              screen Details
                data Item via query Details
                action "Retry"
                  when item.status == "Retry" execute Retry // keep this comment
                  when item.status == "failed" execute Retry
                  otherwise execute Retry
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_rewrite_every_choice_without_rewriting_the_label_or_condition(WorkspaceAuthoringFormatting formatting)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Work", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work")));
        var command = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is CommandSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = command.Handle,
            ExpectedName = "Retry",
            NewName = "RetryAgain",
            Formatting = formatting
        });
        Assert.True(result.Accepted, string.Join(Environment.NewLine, result.Conflicts.Select(conflict => conflict.Message)));
        var action = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<ScreenGuardedActionSyntax>().Single();
        action.Alternatives.Select(alternative => alternative.Command).ShouldContainOnly("RetryAgain", "RetryAgain");
        action.Otherwise!.Command.ShouldEqual("RetryAgain");
        action.Label.ShouldEqual("Retry");
        ((LiteralExpressionSyntax)((ComparisonConditionSyntax)action.Alternatives.First().Condition).Right).Value.ShouldEqual("Retry");
    }
}
