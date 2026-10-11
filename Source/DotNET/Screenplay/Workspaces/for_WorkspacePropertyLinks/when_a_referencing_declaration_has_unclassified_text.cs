// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_referencing_declaration_has_unclassified_text : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source.Replace("screen List", "screen List\n        title \"NAME\""));
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Single(link => link.Reference.Role == "unclassified");

    [Fact] void should_report_incomplete_evidence() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_retain_the_referencing_owner() => _link.Reference.Owner.Address.ShouldEqual("Projects.Registration.Lookup.List");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
    [Fact] void should_explain_the_missing_classification() => _link.Reference.SourceConfidenceReasons.ShouldNotBeEmpty();
    [Fact] void should_make_the_property_incomplete() => _links.Complete(_links.Find("Projects.Registration.Lookup.Project.name").Single()).ShouldBeFalse();
}
