// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_command_event_behavior : given.a_semantic_binder
{
    const string Source =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
          validate
            not empty message "Project name is required"
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                validate
                  name not empty message "Project name is required"
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_event_contract() => Event.Name.ShouldEqual("ProjectRegistered");
    [Fact] void should_bind_the_initial_event_contract_revision() => Event.Revision.ShouldEqual(EventContractRevision.Initial);
    [Fact] void should_bind_concept_not_empty_validation() => _result.Value!.Model.Application.Concepts.Single(_ => _.Name == "ProjectName").Validations.Single().Kind.ShouldEqual(SemanticValidationRuleKind.NotEmpty);
    [Fact] void should_bind_the_command_identifier() => Command.Properties.Single(_ => _.Name == "projectId").IsIdentifier.ShouldBeTrue();
    [Fact] void should_bind_not_empty_validation() => Command.Validations.Single().Kind.ShouldEqual(SemanticValidationRuleKind.NotEmpty);
    [Fact] void should_bind_the_validation_message() => Command.Validations.Single().Message.ShouldEqual("Project name is required");
    [Fact] void should_bind_the_produced_event() => Command.Produces.Single().EventContract.ShouldEqual(Event.Id);
    [Fact] void should_bind_the_event_destination() => ((SemanticResolvedExpression)Command.Produces.Single().Destination!).Target.ShouldEqual(Command.Properties.Single(_ => _.Name == "projectId").Id);
    [Fact] void should_bind_every_event_mapping() => Command.Produces.Single().Mappings.Length.ShouldEqual(2);
    [Fact] void should_map_every_behavior_declaration_to_source() => _result.Value!.SourceMap.Entries.Length.ShouldEqual(12);

    SemanticCommand Command => Slice.Commands.Single();
    SemanticEventContract Event => Slice.Events.Single();
    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
}
