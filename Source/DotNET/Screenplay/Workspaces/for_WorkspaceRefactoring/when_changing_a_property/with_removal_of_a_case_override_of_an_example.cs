// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_a_case_override_of_an_example : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(ExampleSource.Replace("specification Audit", "specification Audit\n        parameter input String\n        case Ready input = \"Grace\"", StringComparison.Ordinal).Replace("then Expected", "then Expected\n          name = case.input", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove") with { EventNeverPersisted = true });

    [Fact] void should_accept_the_disclosed_removal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_disclose_only_the_effective_override() => Result.PropertyReport.Changed.Where(change => change.WeakensAssertion).Select(change => change.Location.Line).ShouldContainOnly(17);
    [Fact] void should_not_remove_shared_case_data() => WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationCaseSyntax>().Single().Values.Single().Property.ShouldEqual("input");
}
