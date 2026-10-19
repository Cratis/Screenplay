// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_a_specification_asserts_the_copy : given.an_identifier_payload_copy_workspace
{
    const string Specification = "      specification Renames\n        when Rename\n          projectId = \"00000000-0000-0000-0000-000000000001\"\n        then Renamed\n          projectId = \"00000000-0000-0000-0000-000000000001\"\n          name = \"something\"\n";

    void Establish() => Create(InlineSource + Specification);

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept_the_disclosed_removal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_disclose_the_weakened_assertion() => Result.PropertyReport!.Changed.Single(change => change.Role == "assertion").WeakensAssertion.ShouldBeTrue();
    [Fact] void should_keep_the_rest_of_the_assertion() => Result.Workspace!.Documents.Single().Text.Contains("then Renamed\n          name = \"something\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_extract_the_event() => Result.PropertyReport!.ExtractedInlineEvents.Single().Name.ShouldEqual("Renamed");
}
