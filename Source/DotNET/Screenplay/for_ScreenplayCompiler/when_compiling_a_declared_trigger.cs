// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// A trigger the domain never raises. The document says the name exists and what an occurrence carries; what
/// makes one occur is the integration's business, and the compiler never asks.
/// </summary>
public class when_compiling_a_declared_trigger : given.a_compiler
{
    const string Source =
        """
        trigger GitHubIssueCreated
          description "GitHub reported a new issue on a watched repository"
          repository
          issue Issue

        module Support
          feature Triage
            slice Automation HandleImportantIssue
              reaction HandleImportantIssue
                when GitHubIssueCreated
                  repository
                  issue
                where issue.labels == "important"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_declare_the_trigger() => Trigger.Name.ShouldEqual("GitHubIssueCreated");
    [Fact] void should_carry_the_trigger_description() => Trigger.Description.ShouldEqual("GitHub reported a new issue on a watched repository");
    [Fact] void should_carry_every_value_the_trigger_provides() => Trigger.Data.Select(_ => _.Name).ShouldContainOnly("repository", "issue");
    [Fact] void should_leave_an_unstated_type_unstated() => Trigger.Data.First().Type.ShouldBeNull();
    [Fact] void should_carry_a_stated_type() => Trigger.Data.Last().Type!.Name.ShouldEqual("Issue");
    [Fact] void should_resolve_the_trigger_the_reaction_names() => ((NamedTriggerSourceSyntax)Reaction.Triggers.Single().Source).Name.ShouldEqual("GitHubIssueCreated");
    [Fact] void should_carry_the_values_the_reaction_takes() => Reaction.Triggers.Single().Data.Select(_ => _.Name).ShouldContainOnly("repository", "issue");
    [Fact] void should_carry_the_condition_that_narrows_it() => ((ComparisonConditionSyntax)Reaction.Where!).Left.ShouldEqual("issue.labels");

    TriggerSyntax Trigger => _result.Value!.Triggers!.Single();

    ReactionSyntax Reaction =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
}
