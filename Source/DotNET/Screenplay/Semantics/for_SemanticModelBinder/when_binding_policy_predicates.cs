// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_policy_predicates : given.a_semantic_binder
{
    const string Source =
        """
        policy CustomAccess
          ```csharp
          return context.Identity.IsAuthenticated;
          ```
        policy AlwaysAllowed
          require authenticated
        policy ManagersOnly
          require role "Manager"
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                department String
                authorize CustomAccess
              specification TryingToFile
                given caller
                  authenticated
                when FileReport
                  department = "Finance"
                then denied
            slice StateView ReportLookup
              readmodel Report
                id Uuid
              query ReportById => Report?
                by id Uuid
                authorize CustomAccess
              specification LookingUpReport
                given caller
                  authenticated
                then query ReportById
                  arguments
                    id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then denied
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticExecutionPlan _plan = null!;

    void Because()
    {
        _result = Bind(Source);
        if (_result.Success) _plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
    }

    [Fact] void should_bind_a_pure_policy_predicate() => _result.ImplementationRequirements.Single().RequiredCapability.ShouldEqual("pure");
    [Fact] void should_bind_v3() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_reference_the_requirement() => ((SemanticOpaquePolicyCondition)_result.Value!.Model.Application.Policies.Single(policy => policy.Name == "CustomAccess").Condition).RequirementId.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_round_trip_the_policy() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model))).SequenceEqual(SemanticModelSerializer.Serialize(_result.Value!.Model)).ShouldBeTrue();
    [Fact] void should_report_unsupported_for_a_guarded_command() => AssertUnsupported("TryingToFile");
    [Fact] void should_report_unsupported_for_a_guarded_query() => AssertUnsupported("LookingUpReport");
    [Fact] void should_keep_an_unused_attachment_from_affecting_a_portable_gate()
    {
        var source = Source.Replace("authorize CustomAccess", "authorize AlwaysAllowed", StringComparison.Ordinal);
        var compilation = Bind(source);
        compilation.Diagnostics.ShouldBeEmpty();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(value => value.Name == "TryingToFile").Id);
        result.Execution.ShouldBeOfExactType<SemanticAccepted>();
    }
    [Fact] void should_use_the_same_identity_for_inline_and_file_attachments()
    {
        var inline = _result.ImplementationRequirements.Single();
        foreach (var path in new[] { "Policies/CustomAccess.cs", "Other/CustomAccess.cs" })
        {
            var source = Source.Replace("```csharp\n  return context.Identity.IsAuthenticated;\n  ```", $"file {path}", StringComparison.Ordinal);
            var result = Bind(source);
            result.Diagnostics.ShouldBeEmpty();
            var requirement = result.ImplementationRequirements.Single();
            requirement.RequirementId.ShouldEqual(inline.RequirementId);
            requirement.ContextVersion.ShouldEqual(1u);
            requirement.ResultVersion.ShouldEqual(1u);
            requirement.RequiredCapability.ShouldEqual("pure");
            ((SemanticOpaquePolicyCondition)result.Value!.Model.Application.Policies.Single(policy => policy.Name == "CustomAccess").Condition).RequirementId.ShouldEqual(inline.RequirementId);
        }
    }
    [Fact] void should_reject_duplicate_policy_requirement_identities()
    {
        var model = _result.Value!.Model;
        var duplicate = new SemanticPolicy("AnotherPolicy", new SemanticOpaquePolicyCondition(_result.ImplementationRequirements.Single().RequirementId));
        var application = model.Application with { Policies = model.Application.Policies.Add(duplicate) };
        var failure = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application));
        failure.ShouldBeOfExactType<InvalidSemanticContract>();
        failure.Message.ShouldEqual("Policies have duplicate requirement identities.");
    }
    [Fact] void should_activate_v3_with_only_an_opaque_policy()
    {
        var model = _result.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with { Commands = [], Queries = [], Specifications = [] });
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application).SemanticVersion.ShouldEqual(SemanticVersion.V3);
        var failure = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application));
        failure.ShouldBeOfExactType<InvalidSemanticContract>();
        failure.Message.ShouldEqual("Policy 'CustomAccess' requires ESM v3 and a requirement identity.");
    }
    [Fact] void should_only_activate_v3_when_the_attachment_is_used()
    {
        var source = Source.Replace("policy CustomAccess\n  ```csharp\n  return context.Identity.IsAuthenticated;\n  ```\n", string.Empty, StringComparison.Ordinal)
            .Replace("authorize CustomAccess", "authorize AlwaysAllowed", StringComparison.Ordinal);
        Bind(source).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
    [Fact] void should_preserve_the_distinct_authorization_composition_outcomes()
    {
        foreach (var (expression, expected) in new[]
        {
            ("ManagersOnly and CustomAccess", SemanticPolicyOutcome.Deny),
            ("CustomAccess and ManagersOnly", SemanticPolicyOutcome.Unsupported),
            ("AlwaysAllowed or CustomAccess", SemanticPolicyOutcome.Allow),
            ("CustomAccess or AlwaysAllowed", SemanticPolicyOutcome.Unsupported),
            ("AlwaysAllowed and CustomAccess", SemanticPolicyOutcome.Unsupported),
            ("CustomAccess or ManagersOnly", SemanticPolicyOutcome.Unsupported)
        })
        {
            var source = Source.Replace("authorize CustomAccess", $"authorize {expression}", StringComparison.Ordinal);
            var compilation = Bind(source);
            compilation.Diagnostics.ShouldBeEmpty();
            var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
            var command = plan.Commands.Values.Single();
            var decision = SemanticPolicyEvaluation.Evaluate(
                command.Authorization,
                plan,
                new(true, [], []),
                new Dictionary<string, SemanticValue> { ["department"] = SemanticValue.Text("Finance") },
                null,
                command.Properties);
            decision.Outcome.ShouldEqual(expected);
            var request = SemanticExecutionRequest.Create(
                command.Id,
                [new(command.Properties.Single().Id, SemanticValue.Text("Finance"))],
                []) with { Caller = new(true, [], []) };
            var execution = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
            switch (expected)
            {
                case SemanticPolicyOutcome.Deny:
                    ((SemanticRejected)execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
                    break;
                case SemanticPolicyOutcome.Allow:
                    execution.ShouldBeOfExactType<SemanticAccepted>();
                    break;
                case SemanticPolicyOutcome.Unsupported:
                    ((SemanticUnsupported)execution).Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
                    break;
            }
        }
    }

    [Fact] void should_evaluate_enclosing_gates_in_module_feature_construct_order()
    {
        var source = Source.Replace("module Portal\n  feature Reports", "module Portal\n  authorize CustomAccess\n  feature Reports\n    authorize ManagersOnly", StringComparison.Ordinal)
            .Replace("authorize CustomAccess\n", "authorize AlwaysAllowed\n", StringComparison.Ordinal);

        // Keep the opaque module gate first, followed by a portable feature denial.
        source = source.Replace("module Portal\n  authorize AlwaysAllowed", "module Portal\n  authorize CustomAccess", StringComparison.Ordinal);
        var compilation = Bind(source);
        compilation.Diagnostics.ShouldBeEmpty();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var decision = SemanticPolicyEvaluation.Evaluate(
            command.Authorization,
            plan,
            new(true, [], []),
            new Dictionary<string, SemanticValue>(),
            null,
            command.Properties);
        decision.Outcome.ShouldEqual(SemanticPolicyOutcome.Unsupported);
    }

    void AssertUnsupported(string name)
    {
        var result = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single(value => value.Name == name).Id);
        result.Passed.ShouldBeFalse();
        result.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        var unsupported = (SemanticUnsupported)result.Execution;
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        unsupported.Details.ShouldEqual("Policy 'CustomAccess' has an opaque predicate and requires a target provider.");
    }
}
