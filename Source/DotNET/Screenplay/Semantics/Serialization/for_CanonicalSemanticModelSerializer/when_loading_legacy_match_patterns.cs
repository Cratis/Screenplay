// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_loading_legacy_match_patterns : a_valid_semantic_model
{
    [Theory]
    [InlineData("(?<x>a)(?<x>b)", "duplicate group name")]
    [InlineData("(?=a)b", "lookaround")]
    [InlineData("[", "unterminated character class")]
    void should_load_then_classify_unsupported_validation_and_generation(string pattern, string construct)
    {
        var rule = new SemanticValidationRule(_commandNamePropertyId, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single() with { Validations = [rule] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(slice with { Commands = [command] }));
        var bytes = SemanticModelSerializer.Serialize(model);
        var restored = SemanticModelSerializer.Deserialize(bytes);
        SemanticModelSerializer.Serialize(restored).SequenceEqual(bytes).ShouldBeTrue();
        var restoredRule = restored.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Commands).Single(_ => _.Id == _commandId).Validations.Single();
        restoredRule.Operand.ShouldEqual(SemanticValue.Text(pattern));
        var compilation = SemanticExecutionPlan.Compile(restored);
        compilation.Success.ShouldBeFalse();
        var issue = compilation.Issues.Single();
        issue.Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedValidation);
        issue.Details.ShouldEqual("outside the portable match dialect: " + construct);
        RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ab" }, SemanticPrimitiveType.Text, restoredRule)
            .ShouldEqual(RuleConsistencyResult.Unproven("outside the portable match dialect: " + construct));
    }
}
