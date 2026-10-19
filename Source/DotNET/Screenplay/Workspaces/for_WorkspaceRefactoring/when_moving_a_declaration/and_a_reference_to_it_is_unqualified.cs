// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_a_reference_to_it_is_unqualified : for_WorkspaceRefactoring.given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            module Projects
              feature Source
                slice StateView Listing
                  readmodel Item
                    name String
                  query Items => Item[]
                  screen Home
                    on enter
                      execute Save
                        on success
                          refresh Items
                slice StateChange Saving
                  command Save
              feature Other
                slice StateView Listing
                  readmodel OtherItem
                    name String
                  query Items => OtherItem[]
              feature Destination
                slice StateView Lookup
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because()
    {
        var application = Workspace.IdentityCatalog.Application;
        _result = Workspace.ProposeMove(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = SemanticAddress.ForQuery(SemanticAddress.ForSlice(application, "Projects", "Source", "Listing"), "Items"),
            NewParent = SemanticAddress.ForSlice(application, "Projects", "Destination", "Lookup"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
    }

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_ask_to_qualify_the_reference_first() => _result.Conflicts.Single().Message.ShouldContain("qualify the reference to 'Items' as 'Projects.Source.Listing.Items' first");
}
