// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_several_properties_on_one_event : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create(Source
        .Replace("        produces Audited\n          for id\n          name = name", "        produces Audited\n          for id\n          name = name\n          alias = name", StringComparison.Ordinal)
        .Replace("      event Audited generation 2\n        name String", "      event Audited generation 2\n        name String\n        alias String", StringComparison.Ordinal)
        .Replace("        then Audited\n          name = \"Ada\"", "        then Audited\n          name = \"Ada\"\n          alias = \"Ada\"", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove", 2) with
    {
        NewGeneration = true,
        AlsoRemove = [new(Property("Audited", "alias", 2).Handle, "alias")]
    });

    [Fact] void should_accept_all_selected_properties() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_advance_the_owner_exactly_once() => Result.PropertyReport.Generations.Single().To.ShouldEqual(3u);
    [Fact] void should_keep_the_old_payload_properties() => Candidate.Contains("alias String", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_both_old_property_identities() => Result.Workspace.IdentityCatalog.ResolveSemanticAssignment(Property("Audited", "alias", 2).Address).Id.ShouldEqual(Property("Audited", "alias", 2).SemanticId!.Value);
}
