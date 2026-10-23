// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_its_reaction_specification_uses_an_unqualified_reference : for_WorkspaceRefactoring.given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            module Projects
              feature Source
                slice Automation Listing
                  event Recorded
                    name String
                  reaction Notify
                    when Recorded
                  specification Redelivery
                    given Recorded
                      name = "A"
                    when redelivered Recorded to Notify
                    then no events
                slice Automation Alerts
                  reaction Notify
                    when Recorded
              feature Destination
                slice Automation Lookup
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because()
    {
        var application = Workspace.IdentityCatalog.Application;
        _result = Workspace.ProposeMove(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = SemanticAddress.ForReaction(SemanticAddress.ForSlice(application, "Projects", "Source", "Listing"), "Notify"),
            NewParent = SemanticAddress.ForSlice(application, "Projects", "Destination", "Lookup"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
    }

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_carry_the_specification() => _result.MoveReport!.SpecificationsCarried.Select(address => address.Name).ShouldContainOnly("Redelivery");
    [Fact] void should_keep_the_unqualified_reference() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationSyntax>().Single().WhenRedelivered!.Reaction.ShouldEqual("Notify");
}
