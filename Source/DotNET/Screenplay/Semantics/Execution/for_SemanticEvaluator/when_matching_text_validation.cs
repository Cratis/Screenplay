// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_matching_text_validation : a_valid_semantic_model
{
    bool _planAdmitsMatches;
    SemanticExecutionResult _accepted;
    SemanticExecutionResult _rejected;
    bool _substring;
    bool _anchored;
    bool _whole;
    bool _email;
    bool _missingDomain;
    bool _doubleAt;
    bool _whitespace;

    void Because()
    {
        var rule = new SemanticValidationRule(_commandNamePropertyId, SemanticValidationRuleKind.Matches, SemanticValue.Text("Screen"), null);
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single() with { Validations = [rule] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(slice with { Commands = [command] }));
        var compilation = SemanticExecutionPlan.Compile(model);
        _planAdmitsMatches = compilation.Success;
        var plan = compilation.Plan!;
        var values = plan.Specifications.Values.Single(_ => _.Name == "registers a project").When!.Values;
        var evaluator = new SemanticEvaluator();
        _accepted = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(_commandId, values, []));
        _rejected = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(
            _commandId,
            [.. values.Select(_ => _.TargetProperty == _commandNamePropertyId ? _ with { Value = SemanticValue.Text("Other") } : _)],
            []));
        rule = rule with { Operand = SemanticValue.Text("INV-") };
        _substring = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("pre-INV-12"));
        rule = rule with { Operand = SemanticValue.Text("^INV-[0-9]+$") };
        _anchored = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("pre-INV-12"));
        _whole = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("INV-12"));
        rule = rule with { Operand = SemanticValue.Text(SemanticMatchPattern.Email) };
        _email = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("user.name@example.co.uk"));
        _missingDomain = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("user@example"));
        _doubleAt = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("user@@example.com"));
        _whitespace = SemanticValidationRules.Satisfies(rule, SemanticValue.Text("user name@example.com"));
    }

    [Fact] void should_admit_a_match_in_the_execution_plan() => _planAdmitsMatches.ShouldBeTrue();
    [Fact] void should_accept_a_matching_command() => _accepted.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_reject_a_nonmatching_command() => _rejected.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_match_a_substring_without_anchors() => _substring.ShouldBeTrue();
    [Fact] void should_reject_a_partial_match_with_anchors() => _anchored.ShouldBeFalse();
    [Fact] void should_match_the_whole_value_with_anchors() => _whole.ShouldBeTrue();
    [Fact] void should_accept_an_email_address() => _email.ShouldBeTrue();
    [Fact] void should_reject_an_email_without_a_dot_in_the_domain() => _missingDomain.ShouldBeFalse();
    [Fact] void should_reject_multiple_at_signs() => _doubleAt.ShouldBeFalse();
    [Fact] void should_reject_whitespace() => _whitespace.ShouldBeFalse();
}
