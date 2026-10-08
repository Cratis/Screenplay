// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_interaction_targets : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            behavior Run
              parameter command
              parameter query
              parameter screen
              on click
                execute command
                  on success
                    refresh query
                    navigate to screen
            module Projects
              feature Source
                slice StateView Moving
                  command Save
                  readmodel Item
                    name String
                  query Items => Item[]
                  screen Home
              feature Destination
              feature Clients
                slice StateView Client
                  screen Client
                    on enter
                      execute Projects.Source.Moving.Save
                        on success
                          refresh Projects.Source.Moving.Items
                          navigate to Projects.Source.Moving.Home
                    uses Run
                      command Projects.Source.Moving.Save
                      query Projects.Source.Moving.Items
                      screen Projects.Source.Moving.Home
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Source", "Moving"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_repair_all_three_operands_and_all_three_parameter_arguments() => _result.MoveReport.ReferenceRepairs.Length.ShouldEqual(6);
    [Fact] void should_repair_navigation() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<NavigateActionSyntax>().Single(action => action.Screen != "screen").Screen.ShouldEqual("Projects.Destination.Moving.Home");
    [Fact] void should_repair_execute() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<ExecuteCommandActionSyntax>().Single(action => action.Command != "command").Command.ShouldEqual("Projects.Destination.Moving.Save");
    [Fact] void should_repair_refresh() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<RefreshQueryActionSyntax>().Single(action => action.Query != "query").Query.ShouldEqual("Projects.Destination.Moving.Items");
    [Fact] void should_not_rewrite_parameter_operands() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<BehaviorSyntax>().Single(behavior => behavior.Name == "Run").Bindings.Single().Actions.OfType<ExecuteCommandActionSyntax>().Single().Command.ShouldEqual("command");
    [Fact] void should_repair_argument_values_at_the_use_site() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Select(entry => entry.Node).OfType<BehaviorArgumentSyntax>().Select(argument => argument.Value).ShouldContainOnly("Projects.Destination.Moving.Save", "Projects.Destination.Moving.Items", "Projects.Destination.Moving.Home");
}
