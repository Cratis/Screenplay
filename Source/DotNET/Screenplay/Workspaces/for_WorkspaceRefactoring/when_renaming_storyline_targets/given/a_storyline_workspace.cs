// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_storyline_targets.given;

public class a_storyline_workspace : for_WorkspaceRefactoring.given.a_refactoring_workspace
{
    protected const string Source = """
        concept ItemId : Uuid
        module Returns
          feature Requests
            slice StateChange RequestReturn
              command RequestReturn
                itemId ItemId
                produces ReturnRequested
                  itemId = itemId
              event ReturnRequested
                itemId ItemId
        storyline ReturnAnItem "A customer returns an item"
          module Returns
          feature Returns.Requests
          slice Requests.RequestReturn
          command RequestReturn "Asks"
          event ReturnRequested
        """;

    protected WorkspaceAuthoringResult Result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Returns", [Document("returns", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Returns")));

    protected string Text => Result.Workspace.Documents.Single().Text;
}
