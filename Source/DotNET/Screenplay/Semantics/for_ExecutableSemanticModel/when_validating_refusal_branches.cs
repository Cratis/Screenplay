// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_refusal_branches : Specification
{
    [Theory]
    [InlineData("unknown", null)]
    [InlineData("validation", "ClaimIsUnique")]
    [InlineData("constraint", "Missing")]
    void should_refuse_malformed_or_unresolved_selectors(string selector, string? constraint) => Reject(invocation => invocation with { OnRefused = [invocation.OnRefused[0] with { Selector = selector, Constraint = constraint }] });

    [Fact] void should_refuse_default_branch_productions() => Reject(invocation => invocation with { OnRefused = [new("any", null, default)] });
    [Fact] void should_refuse_default_branch_arrays() => Reject(invocation => invocation with { OnRefused = default });
    [Fact] void should_refuse_constraint_values_in_bare_branches() => Reject(invocation => invocation with { OnRefused = [invocation.OnRefused[0] with { Selector = "any", Constraint = null }] });
    [Fact] void should_refuse_refusal_values_in_invocation_mappings() => Reject(invocation => invocation with { Mappings = [invocation.Mappings[0] with { Source = new SemanticRefusalExpression("reason", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)) }] });
    [Fact] void should_refuse_unknown_members() => Reject(WithExpression(new("unknown", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));
    [Fact] void should_refuse_non_string_expression_types() => Reject(WithExpression(new("reason", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber))));
    [Fact] void should_refuse_optional_expression_types() => Reject(WithExpression(new("reason", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true))));
    [Fact] void should_refuse_branch_routes() => Reject(invocation => invocation with { OnRefused = [invocation.OnRefused[0] with { Produces = [invocation.OnRefused[0].Produces[0] with { Route = new(default, default) }] }] });

    static Func<SemanticInvocation, SemanticInvocation> WithExpression(SemanticRefusalExpression expression) => invocation =>
    {
        var branch = invocation.OnRefused[0];
        var produced = branch.Produces[0];

        return invocation with { OnRefused = [branch with { Produces = [produced with { Mappings = produced.Mappings.SetItem(0, produced.Mappings[0] with { Source = expression }) }] }] };
    };

    static void Reject(Func<SemanticInvocation, SemanticInvocation> change)
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV19().Application;
        var module = application.Modules[0];
        var feature = module.Features.Single(feature => feature.Name == "InvocationRefusals");
        var slice = feature.Slices[0];
        var reaction = slice.Reactions[0];
        var trigger = reaction.Triggers[0];
        var changed = slice with { Reactions = [reaction with { Triggers = [trigger with { Invokes = [change(trigger.Invokes[0])] }] }] };
        var updated = application with { Modules = application.Modules.SetItem(0, module with { Features = module.Features.SetItem(module.Features.IndexOf(feature), feature with { Slices = [changed] }) }) };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V19, SemanticVersion.V19, updated)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
