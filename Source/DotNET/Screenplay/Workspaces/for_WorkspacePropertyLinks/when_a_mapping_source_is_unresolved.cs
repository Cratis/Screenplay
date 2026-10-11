// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_mapping_source_is_unresolved : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source.Replace("name = name", "name = missing", StringComparison.Ordinal));
    void Because()
    {
        var property = _links.Find("Projects.Registration.Register.Registered.name").Single();
        _complete = _links.Complete(property);
        _link = _links.Incoming(property).Single(link => link.Reference.Role == "mapping-target");
    }

    [Fact] void should_mark_the_property_link_set_incomplete() => _complete.ShouldBeFalse();
    [Fact] void should_retain_the_unresolved_mapping_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
    [Fact] void should_not_invent_the_other_end() => _link.OtherEnd.ShouldBeNull();
}
