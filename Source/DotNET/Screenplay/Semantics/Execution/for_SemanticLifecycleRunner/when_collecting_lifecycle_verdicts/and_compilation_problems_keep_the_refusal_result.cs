// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_collecting_lifecycle_verdicts;

public class and_compilation_problems_keep_the_refusal_result : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Establish()
    {
        // The binder refuses non-portable patterns; programmatic semantic models retain them.
        var concept = Model.Application.Concepts.Single();
        ReplaceApplication(Model.Application with
        {
            Concepts = [concept with { Primitive = SemanticPrimitiveType.Text, Validations = [new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("(?=a)a"), null)] }]
        });
    }

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_a_result_for_every_specification() => _results.Length.ShouldEqual(7);
    [Fact] void should_keep_the_first_owner() => _results[0].Owner.ShouldEqual("InvoiceLifecycle");
    [Fact] void should_keep_the_first_specification() => _results[0].Name.ShouldEqual("SendingStartsTheCollection");
    [Fact] void should_keep_the_refusal_text() => _results[0].Failures.ShouldContainOnly("The model cannot be planned for execution.");
    [Fact] void should_refuse_every_specification() => _results.All(result => !result.Passed).ShouldBeTrue();
}
