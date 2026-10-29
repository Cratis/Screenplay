// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_provider_cannot_execute : given.a_command_handler
{
    [Theory]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("duplicate")]
    [InlineData("wrong-role")]
    [InlineData("result-v2")]
    [InlineData("context-v2")]
    [InlineData("pure")]
    [InlineData("pending")]
    [InlineData("unresolved")]
    void should_not_invoke_unverified_source(string defect)
    {
        var body = Bodies.Single();
        SemanticImplementationBody[] bodies = defect switch
        {
            "stale" => [body with { Content = "stale" }],
            "duplicate" => [body, body],
            "wrong-role" => [body with { Requirement = body.Requirement with { Role = SemanticImplementationRole.RulePredicate } }],
            "result-v2" => [body with { Requirement = body.Requirement with { ResultVersion = 2 } }],
            "context-v2" => [body with { Requirement = body.Requirement with { ContextVersion = 2 } }],
            "pure" => [body with { Requirement = body.Requirement with { RequiredCapability = "pure" } }],
            "pending" => [body with { Requirement = body.Requirement with { AttachmentResolution = SemanticAttachmentResolution.Pending } }],
            "unresolved" => [body with { Requirement = body.Requirement with { AttachmentResolution = SemanticAttachmentResolution.UnresolvedFile } }],
            _ => []
        };
        var result = (SemanticUnsupported)new SemanticEvaluator(Provider, bodies).Execute(Plan, SemanticWorld.Empty, Request);
        result.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        result.Details.ShouldContain(body.Requirement.RequirementId);
        Contexts.ShouldBeEmpty();
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }

    [Fact]
    void should_keep_the_no_provider_refusal()
    {
        var result = (SemanticUnsupported)new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, Request);
        result.Details.ShouldEqual($"Command 'Place' handler body requires a target (requirement {Bodies.Single().Requirement.RequirementId}); the reference executor does not run implementation bodies.");
    }

    [Fact]
    void should_fail_closed_for_an_existing_rule_only_provider()
    {
        var result = (SemanticUnsupported)new SemanticEvaluator(new rules_only(), Bodies).Execute(Plan, SemanticWorld.Empty, Request);
        result.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
    }

    sealed class rules_only : ISemanticBodyExecutionProvider
    {
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
