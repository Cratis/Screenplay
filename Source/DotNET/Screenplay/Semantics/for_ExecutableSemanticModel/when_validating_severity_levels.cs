// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_severity_levels : a_valid_semantic_model
{
    Exception _invalidRule;
    Exception _invalidRequirement;
    Exception _validWarning;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        _invalidRule = Validate(slice, command with
        {
            Validations = [command.Validations.Single() with { Severity = (SemanticValidationSeverity)99 }]
        });
        var condition = new SemanticComparison(
            new(_commandNamePropertyId, null), SemanticComparisonOperator.NotEqual, new(default, SemanticValue.Text("bad")));
        _invalidRequirement = Validate(slice, command with
        {
            Requirements = [new(condition, "Bad") { Severity = (SemanticValidationSeverity)99 }]
        });
        _validWarning = Validate(slice, command with
        {
            Validations = [command.Validations.Single() with { Severity = SemanticValidationSeverity.Warning }]
        });
    }

    Exception Validate(SemanticSlice slice, SemanticCommand command) => Catch.Exception(() => ExecutableSemanticModel.Create(
        LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(slice with { Commands = [command], Specifications = [] })));

    [Fact] void should_reject_an_unknown_rule_level() => _invalidRule.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unknown_requirement_level() => _invalidRequirement.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_admit_a_warning_level() => _validWarning.ShouldBeNull();
}
#endif
