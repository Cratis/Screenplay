// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_rule_predicates : given.a_semantic_binder
{
    const string Source =
        """
        concept Label : String
          validate
            rule CheckLabel severity warning message $strings.label.invalid
              file Rules/CheckLabel.cs
        type Item
          label Label
        module Orders
          feature Ordering
            slice StateChange Orders
              command PlaceOrder
                items Item[]
                label String
                validate
                  label not empty message "A label is needed"
                  label rule CheckCommand severity information message $strings.command.invalid
                    ```csharp
                    return context.Value != "no";
                    ```
                validate
                  ```csharp
                  yield return "Invalid order";
                  ```
              command OtherOrder
                id Uuid identifier
                label String
                produces OtherPlaced
                  for id
                  id = id
                  label = label
              event OtherPlaced
                id Uuid
                label String
              specification ARejectedOrder
                when PlaceOrder
                  items = []
                  label = ""
                then error "A label is needed"
              specification AnOtherOrder
                when OtherOrder
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  label = "ok"
                then OtherPlaced
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  label = "ok"
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticExecutionPlan _plan = null!;

    void Because()
    {
        _result = Bind(Source);
        if (_result.Success) _plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
    }

    [Fact] void should_bind_v3() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_admit_a_reference_plan() => _plan.ShouldNotBeNull();
    [Fact] void should_preserve_message_severity_and_identity()
    {
        var rule = Command.Validations.Single(validation => validation.Kind == SemanticValidationRuleKind.RulePredicate);
        rule.Name.ShouldEqual("CheckCommand");
        rule.Message.ShouldEqual("$strings.command.invalid");
        rule.Severity.ShouldEqual(SemanticValidationSeverity.Information);
        rule.RequirementId.ShouldEqual(_result.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.RulePredicate && requirement.Member == "label/CheckCommand").RequirementId);
    }

    [Fact] void should_bind_three_pure_attachments() => _result.ImplementationRequirements.All(requirement => requirement.RequiredCapability == "pure").ShouldBeTrue();
    [Fact] void should_report_unsupported_even_if_a_declarative_rule_rejects() => (Run("ARejectedOrder").Execution is SemanticUnsupported unsupported && unsupported.Details.Contains("code validation", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_pass_a_specification_that_cannot_execute() => Run("ARejectedOrder").Passed.ShouldBeFalse();
    [Fact] void should_leave_an_unrelated_specification_executable() => Run("AnOtherOrder").Passed.ShouldBeTrue();
    [Fact] void should_fail_closed_on_a_nested_collection_concept()
    {
        var source = Source.Replace("command OtherOrder\n        id Uuid identifier", "command OtherOrder\n        items Item[]\n        id Uuid identifier", StringComparison.Ordinal)
            .Replace("when OtherOrder\n          id =", "when OtherOrder\n          items = []\n          id =", StringComparison.Ordinal);
        var compilation = Bind(source);
        compilation.Diagnostics.ShouldBeEmpty();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single(value => value.Name == "OtherOrder");
        var request = SemanticExecutionRequest.Create(
            command.Id,
            [new(command.Properties.Single(value => value.Name == "items").Id, SemanticValue.Array([])),
             new(command.Properties.Single(value => value.Name == "id").Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")),
             new(command.Properties.Single(value => value.Name == "label").Id, SemanticValue.Text("valid"))],
            []);
        var result = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        ((SemanticUnsupported)result).Details.ShouldEqual("Rule 'CheckLabel' has an opaque validation predicate and requires a target provider.");
    }

    SemanticCommand Command => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(command => command.Name == "PlaceOrder");
    SemanticSpecificationRun Run(string name)
    {
        var specification = _plan.Specifications.Values.Single(value => value.Name == name);
        return new SemanticSpecificationRunner().Run(_plan, specification.Id);
    }
}
