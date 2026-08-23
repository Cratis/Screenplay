// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.given;

public class a_valid_semantic_model : Specification
{
    protected ExecutableSemanticModel _model;
    protected SemanticApplication _application;
    protected SemanticIdentityCatalog _catalog;
    protected EventContractId _eventContractId;
    protected SemanticId _applicationId;
    protected SemanticId _commandId;
    protected SemanticId _eventId;
    protected SemanticId _eventNamePropertyId;
    protected SemanticId _eventProjectIdPropertyId;
    protected SemanticId _projectIdConceptId;
    protected SemanticId _projectNameConceptId;
    protected SemanticId _queryId;
    protected SemanticId _readModelId;
    protected SemanticId _readModelNamePropertyId;
    protected SemanticId _readModelProjectIdPropertyId;

    void Establish()
    {
        _applicationId = Id(SemanticKind.Application, "Projects");
        _projectIdConceptId = Id(SemanticKind.Concept, "ProjectId");
        _projectNameConceptId = Id(SemanticKind.Concept, "ProjectName");
        _commandId = Id(SemanticKind.Command, "RegisterProject");
        _eventId = Id(SemanticKind.EventContract, "ProjectRegistered");
        _readModelId = Id(SemanticKind.ReadModel, "ProjectSummary");
        _queryId = Id(SemanticKind.Query, "ProjectById");

        var commandProjectIdPropertyId = Id(SemanticKind.Property, "RegisterProject.ProjectId");
        var commandNamePropertyId = Id(SemanticKind.Property, "RegisterProject.Name");
        _eventProjectIdPropertyId = Id(SemanticKind.Property, "ProjectRegistered.ProjectId");
        _eventNamePropertyId = Id(SemanticKind.Property, "ProjectRegistered.Name");
        _readModelProjectIdPropertyId = Id(SemanticKind.Property, "ProjectSummary.ProjectId");
        _readModelNamePropertyId = Id(SemanticKind.Property, "ProjectSummary.Name");

        var eventAddress = Address(SemanticKind.EventContract, "ProjectRegistered");
        _eventContractId = EventContractId.CreateLegacy(eventAddress);
        var eventContract = new SemanticEventContract(
            _eventId,
            _eventContractId,
            EventContractRevision.Initial,
            "ProjectRegistered",
            [
                new(_eventProjectIdPropertyId, "ProjectId", SemanticTypeReference.ForConcept(_projectIdConceptId), false),
                new(_eventNamePropertyId, "Name", SemanticTypeReference.ForConcept(_projectNameConceptId), false)
            ]);
        var command = new SemanticCommand(
            _commandId,
            "RegisterProject",
            [
                new(commandProjectIdPropertyId, "ProjectId", SemanticTypeReference.ForConcept(_projectIdConceptId), true),
                new(commandNamePropertyId, "Name", SemanticTypeReference.ForConcept(_projectNameConceptId), false)
            ],
            [new(commandNamePropertyId, SemanticValidationRuleKind.NotEmpty, null, "Project name is required")],
            [new(
                _eventContractId,
                null,
                SemanticExpression.Path("command.projectId"),
                [
                    new(_eventProjectIdPropertyId, SemanticExpression.Path("command.projectId")),
                    new(_eventNamePropertyId, SemanticExpression.Path("command.name"))
                ])]);
        var readModel = new SemanticReadModel(
            _readModelId,
            "ProjectSummary",
            [
                new(_readModelProjectIdPropertyId, "ProjectId", SemanticTypeReference.ForConcept(_projectIdConceptId), true),
                new(_readModelNamePropertyId, "Name", SemanticTypeReference.ForConcept(_projectNameConceptId), false)
            ]);
        var projection = new SemanticProjection(
            Id(SemanticKind.Projection, "ProjectSummaryProjection"),
            "ProjectSummaryProjection",
            _readModelId,
            [new(
                _eventContractId,
                new(AffectedInstanceCardinality.One, SemanticExpression.Path("event.projectId")),
                [
                    new(_readModelProjectIdPropertyId, SemanticExpression.Path("event.projectId")),
                    new(_readModelNamePropertyId, SemanticExpression.Path("event.name"))
                ])]);
        var query = new SemanticKeyedQuery(
            _queryId,
            "ProjectById",
            new("projectId", SemanticTypeReference.ForConcept(_projectIdConceptId)),
            _readModelId,
            _readModelProjectIdPropertyId,
            SemanticQueryCardinality.ZeroOrOne,
            SemanticQueryDelivery.Snapshot);
        var commandValues = ImmutableArray.Create(
            new SemanticPropertyMapping(commandProjectIdPropertyId, SemanticExpression.TextValue("project-1")),
            new SemanticPropertyMapping(commandNamePropertyId, SemanticExpression.TextValue("Screenplay")));
        var eventValues = ImmutableArray.Create(
            new SemanticPropertyMapping(_eventProjectIdPropertyId, SemanticExpression.TextValue("project-1")),
            new SemanticPropertyMapping(_eventNamePropertyId, SemanticExpression.TextValue("Screenplay")));
        var readModelState = new SemanticSpecificationReadModel(
            _readModelId,
            SemanticExpression.TextValue("project-1"),
            [
                new(_readModelProjectIdPropertyId, SemanticExpression.TextValue("project-1")),
                new(_readModelNamePropertyId, SemanticExpression.TextValue("Screenplay"))
            ]);
        var success = new SemanticSpecification(
            Id(SemanticKind.Specification, "registers a project"),
            "registers a project",
            [],
            [],
            new(_commandId, commandValues),
            [new(_eventContractId, eventValues)],
            [readModelState],
            [new(_queryId, SemanticExpression.TextValue("project-1"), [readModelState])],
            []);
        var rejection = new SemanticSpecification(
            Id(SemanticKind.Specification, "rejects an empty name"),
            "rejects an empty name",
            [],
            [],
            new(
                _commandId,
                [
                    new(commandProjectIdPropertyId, SemanticExpression.TextValue("project-1")),
                    new(commandNamePropertyId, SemanticExpression.TextValue(string.Empty))
                ]),
            [],
            [],
            [],
            [new("validation", "Project name is required")]);
        var stateChange = new SemanticSlice(
            Id(SemanticKind.Slice, "Registration"),
            "Registration",
            SemanticSliceKind.StateChange,
            [eventContract],
            [command],
            [],
            [],
            [],
            [rejection, success]);
        var stateView = new SemanticSlice(
            Id(SemanticKind.Slice, "ProjectSummaries"),
            "ProjectSummaries",
            SemanticSliceKind.StateView,
            [],
            [],
            [readModel],
            [projection],
            [query],
            []);
        var metadataNameProperty = new SemanticProperty(
            Id(SemanticKind.Property, "ProjectMetadata.DisplayName"),
            "DisplayName",
            SemanticTypeReference.ForConcept(_projectNameConceptId),
            false);
        _application = new(
            _applicationId,
            "Projects",
            [
                new(_projectNameConceptId, "ProjectName", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.NotEmpty, null, null)]),
                new(_projectIdConceptId, "ProjectId", SemanticPrimitiveType.Uuid, [], [])
            ],
            [new(Id(SemanticKind.CompositeType, "ProjectMetadata"), "ProjectMetadata", [metadataNameProperty])],
            [new(
                Id(SemanticKind.Module, "Projects"),
                "Projects",
                [new(Id(SemanticKind.Feature, "Projects"), "Projects", [], [stateView, stateChange])])]);
        _model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, _application);
        _catalog = SemanticIdentityCatalog.Create(
            [],
            [new(Address(SemanticKind.Application, "Projects"), _applicationId, SemanticIdentityOrigin.Persisted)],
            [new(eventAddress, _eventContractId, EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)]);
    }

    protected static SemanticAddress Address(SemanticKind kind, string key) =>
        SemanticAddress.Create(kind, [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, key)]);

    protected static SemanticId Id(SemanticKind kind, string key) => SemanticId.Create(Address(kind, key));
}
