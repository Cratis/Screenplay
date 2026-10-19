// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

// Commands, events, read models and queries resolve by unique short name across the application, so moving one
// never changes what an unqualified reference names. A reaction resolves innermost scope first: once 'Notify'
// leaves Listing, the redelivery's unqualified 'Notify' would silently bind to the sibling slice's reaction.
public class and_a_reference_to_it_is_unqualified : for_WorkspaceRefactoring.given.a_refactoring_workspace
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

    string Refusal => string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_ask_to_qualify_the_reference_first() => Assert.True(Refusal.Contains("qualify the reference to 'Notify' as 'Projects.Source.Listing.Notify' first", StringComparison.Ordinal), Refusal);
}
