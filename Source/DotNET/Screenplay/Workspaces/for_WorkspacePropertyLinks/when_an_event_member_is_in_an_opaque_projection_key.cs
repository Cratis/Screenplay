// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_event_member_is_in_an_opaque_projection_key : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse(
            """
            module M
              feature F
                slice StateView S
                  event Changed
                    sourceId Uuid
                  readmodel Account
                    name String
                  projection Accounts => Account
                    no automap
                    from Changed key compute(sourceId)
            """,
            "model.play").Value!;
        var index = new AuthoringIndex();
        index.VisitApplication(application);
        index.Complete(application);
        _links = new(WorkspacePropertyLinks.Entries([application]), index);
    }

    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Changed.sourceId").Single()).Single(link => link.Reference.Role == "key");

    [Fact] void should_retain_unsupported_key_evidence() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_mark_the_property_incomplete() => _links.Complete(_links.Find("M.F.S.Changed.sourceId").Single()).ShouldBeFalse();
    [Fact] void should_retain_the_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Accounts");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}
