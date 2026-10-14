// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticInvocationRefusalEvaluation;

public class when_classifying_refusals : Specification
{
    static SemanticRejected Mixed(SemanticRejectionCategory category) => new(SemanticWorld.Empty, category, "First", " denial ")
    {
        ConstraintFailures = [new("First", "$strings.first"), new("Second", " second ")],
        ValidationFailures = [new(" validation ", SemanticValidationSeverity.Error)]
    };

    [Fact] void should_give_authorization_precedence_over_other_failures() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Unauthorized))!.Reason.ShouldEqual("authorization");
    [Fact] void should_give_constraint_precedence_over_validation() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Validation))!.Reason.ShouldEqual("constraint");
    [Fact] void should_preserve_the_denial_reason_verbatim() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Unauthorized))!.Message.ShouldEqual(" denial ");
    [Fact] void should_match_any_violated_constraint_name() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Constraint))!.Matches(new("constraint", "Second", [])).ShouldBeTrue();
    [Fact] void should_expose_the_first_violated_constraint_name() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Constraint))!.ValueOf("constraint").ShouldEqual(SemanticValue.Text("First"));
    [Fact] void should_join_messages_in_result_order_without_resolving_keys_or_trimming() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Constraint))!.Message.ShouldEqual("$strings.first\n second ");
    [Fact] void should_exclude_authorization_from_bare_refusal() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Unauthorized))!.Matches(new("any", null, [])).ShouldBeFalse();
    [Fact] void should_include_constraints_in_bare_refusal() => SemanticInvocationRefusalEvaluation.Classify(Mixed(SemanticRejectionCategory.Constraint))!.Matches(new("any", null, [])).ShouldBeTrue();

    [Theory]
    [InlineData(SemanticRejectionCategory.Contract)]
    [InlineData(SemanticRejectionCategory.Concurrency)]
    void should_not_classify_non_refusal_rejections(SemanticRejectionCategory category) => SemanticInvocationRefusalEvaluation.Classify(Mixed(category)).ShouldBeNull();

    [Fact] void should_not_classify_unsupported_outcomes() => SemanticInvocationRefusalEvaluation.Classify(new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Command, "opaque")).ShouldBeNull();
    [Fact] void should_not_classify_warning_only_validation_failures() => SemanticInvocationRefusalEvaluation.Classify(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "warning") { ValidationFailures = [new("warning", SemanticValidationSeverity.Warning)] }).ShouldBeNull();
    [Fact] void should_join_only_error_severity_validation_messages() => SemanticInvocationRefusalEvaluation.Classify(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "first") { ValidationFailures = [new("first", SemanticValidationSeverity.Error), new("warning", SemanticValidationSeverity.Warning), new("last", SemanticValidationSeverity.Error)] })!.Message.ShouldEqual("first\nlast");
}
