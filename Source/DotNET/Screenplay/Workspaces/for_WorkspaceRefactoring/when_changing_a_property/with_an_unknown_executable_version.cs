// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_an_unknown_executable_version : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create(Source.Replace("      event UnrelatedFact", "        handler\n          ```csharp\n          return Nothing();\n          ```\n      event UnrelatedFact", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Registered") with { NewGeneration = true });

    [Fact] void should_accept_valid_source_separately() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_not_claim_an_executable_version() => Result.PropertyReport.EsmVersionKnown.ShouldBeFalse();
    [Fact] void should_not_infer_a_version_change() => Result.PropertyReport.EsmVersionChange.ShouldBeNull();
    [Fact] void should_still_disclose_the_selected_generation() => Result.PropertyReport.Generations.Single().To.ShouldEqual(2u);
}
