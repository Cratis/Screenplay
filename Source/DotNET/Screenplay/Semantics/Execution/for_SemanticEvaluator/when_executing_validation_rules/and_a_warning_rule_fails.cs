// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_a_warning_rule_fails : given.a_validated_command_plan
{
    SemanticRejected _rejection;
    SemanticExecutionResult _accepted;
    SemanticRejected _requirementRejection;
    SemanticRejected _multipleFailures;

    void Because()
    {
        var slice = _plan.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var rules = command.Validations.Select(rule => rule.Message == "An order is for something"
            ? rule with { Severity = SemanticValidationSeverity.Warning }
            : rule).ToImmutableArray();
        var name = command.Properties.Single(_ => _.Name == "name").Id;
        var requirement = new SemanticRequirement(
            new SemanticComparison(new(name, null), SemanticComparisonOperator.NotEqual, new(default, SemanticValue.Text("bad"))),
            "Bad name") { Severity = SemanticValidationSeverity.Information };
        var changed = command with { Validations = rules, Requirements = [requirement] };
        var feature = _plan.Model.Application.Modules.Single().Features.Single();
        var module = _plan.Model.Application.Modules.Single();
        var application = _plan.Model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [changed] }] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application)).Plan!;
        _rejection = (SemanticRejected)Execute(("amount", SemanticValue.Number(0)));
        _accepted = Execute(("amount", SemanticValue.Number(1)));
        _requirementRejection = (SemanticRejected)Execute(("name", SemanticValue.Text("bad")));
        _multipleFailures = (SemanticRejected)Execute(("amount", SemanticValue.Number(0)), ("priority", SemanticValue.Number(0)));
    }

    [Fact] void should_reject_even_a_warning() => _rejection.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_failed_rule_message() => _rejection.ValidationFailures.Single().Message.ShouldEqual("An order is for something");
    [Fact] void should_report_the_warning_severity() => _rejection.ValidationFailures.Single().Severity.ShouldEqual(SemanticValidationSeverity.Warning);
    [Fact] void should_accept_the_satisfied_rule() => _accepted.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_reject_an_information_requirement() => _requirementRejection.ValidationFailures.Single().Severity.ShouldEqual(SemanticValidationSeverity.Information);
    [Fact] void should_report_the_requirement_message() => _requirementRejection.ValidationFailures.Single().Message.ShouldEqual("Bad name");
    [Fact] void should_report_each_failed_rule() => _multipleFailures.ValidationFailures.Select(_ => _.Severity).SequenceEqual([SemanticValidationSeverity.Warning, SemanticValidationSeverity.Error]).ShouldBeTrue();
}
