// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_literal_values_in_case_examples : given.a_case_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CaseRequest() with { FixtureValue = "\"Normal\"" });

    [Fact] void should_accept_constant_additions_without_changing_shared_parameters() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_shared_row_intact() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Select(entry => entry.Node).OfType<SpecificationCaseSyntax>().Single().Values.Select(mapping => mapping.Property).ShouldContainOnly("input");
    [Fact] void should_add_to_both_examples_and_both_steps() => Result.PropertyReport.Changed.Count(change => change.Change == "fixtureAdded").ShouldEqual(4);
    [Fact] void should_disclose_the_inherited_assertion() => Result.PropertyReport.Changed.Any(change => change.Role == "assertion" && change.Change == "fixtureAdded").ShouldBeTrue();
    [Fact] void should_pass_every_case() => Run().Failures.ShouldBeEmpty();

    SemanticSpecificationRun Run()
    {
        var model = Result.Workspace.Compilation.Value.Model;
        return new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan, model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().Id);
    }
}
