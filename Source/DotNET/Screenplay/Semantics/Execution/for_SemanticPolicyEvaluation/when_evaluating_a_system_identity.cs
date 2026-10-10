// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

public class when_evaluating_a_system_identity : Specification
{
    [Theory]
    [InlineData("role \"Automation\"", "Automation", true)]
    [InlineData("role \"Automation\"", "Other", false)]
    [InlineData("claim \"actor\" matches \"system\"", "Automation", false)]
    [InlineData("not claim \"actor\" matches \"system\"", "Automation", false)]
    [InlineData("role \"Automation\" or claim \"actor\" matches \"system\"", "Automation", true)]
    [InlineData("claim \"actor\" matches \"system\" or role \"Automation\"", "Automation", true)]
    [InlineData("not claim \"actor\" matches \"system\" and role \"Automation\"", "Automation", false)]
    [InlineData("authenticated", "", true)]
    void should_evaluate_only_authentication_and_exact_roles(string condition, string role, bool allowed)
    {
        var source = $"policy Access\n  require {condition}\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        authorize Access";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SystemIdentity"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("single"), "single", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("SystemIdentity", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var identity = new SemanticReactionIdentity(SemanticReactionIdentityKind.System, role.Length == 0 ? [] : [role]);

        // Even a supplied caller's claims cannot leak into the reaction's declared identity.
        var decision = SemanticPolicyEvaluation.Evaluate(command.Authorization, plan, new(true, ["Other"], [new("actor", "system")]), new Dictionary<string, SemanticValue>(), null, command.Properties, identity);
        decision.Outcome.ShouldEqual(allowed ? SemanticPolicyOutcome.Allow : SemanticPolicyOutcome.Deny);
    }
}
