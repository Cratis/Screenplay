// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_performing;

public class and_using_an_existing_provider : given.a_reaction_effect
{
    SemanticPerformerBodyResult _result;
    async Task Because()
    {
        ISemanticBodyExecutionProvider provider = Provider;
        _result = await provider.PerformAsync(Bodies.Single(), new(Payload, null, null, null, []), TestContext.Current.CancellationToken);
    }
    [Fact] void should_default_to_unsupported() => _result.Diagnostic.ShouldContain("Query performer body");
    [Fact] void should_name_the_requirement() => _result.Diagnostic.ShouldContain(Bodies.Single().Requirement.RequirementId);
    [Fact] void should_not_answer_empty_rows() => _result.Rows.IsDefault.ShouldBeTrue();
    [Fact] void should_not_call_the_reaction_method() => Provider.Calls.ShouldEqual(0);
}
