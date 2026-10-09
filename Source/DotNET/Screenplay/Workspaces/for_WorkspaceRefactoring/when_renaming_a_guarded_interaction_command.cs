// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_guarded_interaction_command
{
    const string Source = """
        module Work
          feature Items
            slice StateView Details
              command Retry
              readmodel Item
                status String
              query Details => Item
              screen Details
                data Item via query Details
                on click
                  when item.status == "Retry"
                    confirm "Retry"
                      on success
                        execute Retry // keep this comment
                  otherwise
                    execute Retry
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_rewrite_nested_and_fallback_commands_without_rewriting_conditions(WorkspaceAuthoringFormatting formatting)
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
        var nodes = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).ToArray();
        nodes.OfType<ExecuteCommandActionSyntax>().Select(action => action.Command).ShouldContainOnly("RetryAgain", "RetryAgain");
        ((LiteralExpressionSyntax)((ComparisonConditionSyntax)nodes.OfType<InteractionAlternativeSyntax>().Single().Condition).Right).Value.ShouldEqual("Retry");
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia) result.Workspace!.Documents[0].Text.ShouldContain("// keep this comment");
    }
}
