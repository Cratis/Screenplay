// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_source_or_provider_is_unavailable : given.a_policy
{
    [Theory]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("duplicate")]
    [InlineData("wrong-role")]
    [InlineData("result-v2")]
    [InlineData("context-v2")]
    [InlineData("impure")]
    [InlineData("pending")]
    [InlineData("unresolved")]
    void should_refuse_unverified_source_without_invoking_the_provider(string problem)
    {
        var body = Bodies.Single();
        var bodies = problem switch
        {
            "stale" => new[] { body with { Content = "stale" } },
            "duplicate" => new[] { body, body },
            "wrong-role" => new[] { body with { Requirement = body.Requirement with { Role = SemanticImplementationRole.RulePredicate } } },
            "result-v2" => new[] { body with { Requirement = body.Requirement with { ResultVersion = 2 } } },
            "context-v2" => new[] { body with { Requirement = body.Requirement with { ContextVersion = 2 } } },
            "impure" => new[] { body with { Requirement = body.Requirement with { RequiredCapability = "provider-defined" } } },
            "pending" => new[] { body with { Requirement = body.Requirement with { AttachmentResolution = SemanticAttachmentResolution.Pending } } },
            "unresolved" => new[] { body with { Requirement = body.Requirement with { AttachmentResolution = SemanticAttachmentResolution.UnresolvedFile } } },
            _ => []
        };
        var result = (SemanticUnsupported)new SemanticEvaluator(Provider, bodies).Execute(Plan, SemanticWorld.Empty, Request);
        result.Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        result.Details.ShouldContain(body.Requirement.RequirementId);
        Contexts.ShouldBeEmpty();
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_the_no_provider_message()
    {
        var result = (SemanticUnsupported)new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, Request);
        result.Details.ShouldEqual("Policy 'Permitted' has an opaque predicate and requires a target provider.");
    }

    [Fact]
    void should_fail_closed_for_an_existing_provider_implementing_only_rules()
    {
        var result = (SemanticUnsupported)new SemanticEvaluator(new rules_only(), Bodies).Execute(Plan, SemanticWorld.Empty, Request);
        result.Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
    }

    sealed class rules_only : ISemanticBodyExecutionProvider
    {
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
