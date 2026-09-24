// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_producing_an_event_with_command_occurrence : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  registeredAt = $context.occurred
                  registeredBy = $context.identity.userName
              event ProjectRegistered
                registeredAt DateTime
                registeredBy String
        """;

    SemanticExecutionResult _result = null!;
    SemanticExecutionResult _withoutOccurrence = null!;
    SemanticExecutionResult _invalidAuditIdentity = null!;
    SemanticCommand _command = null!;
    SemanticEventContract _event = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _model = Bind(Source).Value!.Model;
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single();
        _command = slice.Commands.Single();
        _event = slice.Events.Single();
    }

    void Because()
    {
        var request = SemanticExecutionRequest.Create(
            _command.Id,
            [new(_command.Properties.Single().Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"))],
            []) with { Occurrence = new(new DateTimeOffset(2025, 3, 2, 1, 2, 3, TimeSpan.Zero), "subject", "Auditor", "auditor") };
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        _result = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        _withoutOccurrence = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request with { Occurrence = null });
        var invalid = Bind(Source.Replace("registeredBy String", "registeredBy Uuid", StringComparison.Ordinal)).Value!.Model;
        _invalidAuditIdentity = new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(invalid).Plan!, SemanticWorld.Empty, request);
    }

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_reject_a_missing_occurrence_instead_of_inventing_a_time() => _withoutOccurrence.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_reject_a_value_incompatible_with_the_audit_identity_type() => _invalidAuditIdentity.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_carry_the_typed_source_in_event_context() => ((SemanticAccepted)_result).Facts.Single().Context!.EventSource.Value.ShouldEqual(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
    [Fact] void should_map_the_event_occurrence_time() => Value("registeredAt").ShouldEqual(SemanticValue.Text("2025-03-02T01:02:03.0000000Z"));
    [Fact] void should_map_the_caller_name() => Value("registeredBy").ShouldEqual(SemanticValue.Text("auditor"));

    SemanticValue Value(string name) => ((SemanticAccepted)_result).Facts.Single().Values.Single(value => value.TargetProperty == _event.Properties.Single(property => property.Name == name).Id).Value;
}
