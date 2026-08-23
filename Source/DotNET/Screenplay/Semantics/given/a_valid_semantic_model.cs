// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.given;

public class a_valid_semantic_model : Specification
{
    protected ExecutableSemanticModel _model;
    protected SemanticApplication _application;
    protected SemanticIdentityCatalog _catalog;
    protected ApplicationIdentity _applicationIdentity;
    protected EventContractId _eventContractId;
    protected SemanticId _applicationId;
    protected SemanticId _commandId;
    protected SemanticId _commandNamePropertyId;
    protected SemanticId _commandProjectIdPropertyId;
    protected SemanticId _eventId;
    protected SemanticId _eventNamePropertyId;
    protected SemanticId _eventProjectIdPropertyId;
    protected SemanticId _projectIdConceptId;
    protected SemanticId _projectNameConceptId;
    protected SemanticId _queryArgumentId;
    protected SemanticId _queryId;
    protected SemanticId _readModelId;
    protected SemanticId _readModelNamePropertyId;
    protected SemanticId _readModelProjectIdPropertyId;

    void Establish()
    {
        _applicationIdentity = ApplicationIdentity.Create("Projects");
        _applicationId = Id(SemanticKind.Application, "Projects");
        _projectIdConceptId = Id(SemanticKind.Concept, "ProjectId");
        _projectNameConceptId = Id(SemanticKind.Concept, "ProjectName");
        _commandId = Id(SemanticKind.Command, "RegisterProject");
        _eventId = Id(SemanticKind.EventContract, "ProjectRegistered");
        _readModelId = Id(SemanticKind.ReadModel, "ProjectSummary");
        _queryId = Id(SemanticKind.Query, "ProjectById");

        _commandProjectIdPropertyId = Id(SemanticKind.Property, "RegisterProject.ProjectId");
        _commandNamePropertyId = Id(SemanticKind.Property, "RegisterProject.Name");
        _queryArgumentId = Id(SemanticKind.QueryArgument, "ProjectById.projectId");
        _eventProjectIdPropertyId = Id(SemanticKind.Property, "ProjectRegistered.ProjectId");
        _eventNamePropertyId = Id(SemanticKind.Property, "ProjectRegistered.Name");
        _readModelProjectIdPropertyId = Id(SemanticKind.Property, "ProjectSummary.ProjectId");
        _readModelNamePropertyId = Id(SemanticKind.Property, "ProjectSummary.Name");

        var eventAddress = Address(SemanticKind.EventContract, "ProjectRegistered");
        _eventContractId = EventContractId.CreateLegacy(_applicationIdentity, eventAddress.Name);
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
                new(_commandProjectIdPropertyId, "ProjectId", SemanticTypeReference.ForConcept(_projectIdConceptId), true),
                new(_commandNamePropertyId, "Name", SemanticTypeReference.ForConcept(_projectNameConceptId), false)
            ],
            [new(_commandNamePropertyId, SemanticValidationRuleKind.NotEmpty, null, "Project name is required")],
            [new(
                _eventId,
                null,
                SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandProjectIdPropertyId),
                [
                    new(_eventProjectIdPropertyId, SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandProjectIdPropertyId)),
                    new(_eventNamePropertyId, SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandNamePropertyId))
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
                _eventId,
                new(AffectedInstanceCardinality.One, SemanticExpression.Property(SemanticExpressionRootKind.Event, _eventProjectIdPropertyId)),
                [
                    new(_readModelProjectIdPropertyId, SemanticExpression.Property(SemanticExpressionRootKind.Event, _eventProjectIdPropertyId)),
                    new(_readModelNamePropertyId, SemanticExpression.Property(SemanticExpressionRootKind.Event, _eventNamePropertyId))
                ])]);
        var query = new SemanticKeyedQuery(
            _queryId,
            "ProjectById",
            new(_queryArgumentId, "projectId", SemanticTypeReference.ForConcept(_projectIdConceptId)),
            _readModelId,
            _readModelProjectIdPropertyId,
            SemanticQueryCardinality.ZeroOrOne,
            SemanticQueryDelivery.Snapshot);
        var projectId = SemanticValue.Text("00000000-0000-0000-0000-000000000001");
        var commandValues = ImmutableArray.Create(
            new SemanticPropertyValue(_commandProjectIdPropertyId, projectId),
            new SemanticPropertyValue(_commandNamePropertyId, SemanticValue.Text("Screenplay")));
        var eventValues = ImmutableArray.Create(
            new SemanticPropertyValue(_eventProjectIdPropertyId, projectId),
            new SemanticPropertyValue(_eventNamePropertyId, SemanticValue.Text("Screenplay")));
        var readModelState = new SemanticSpecificationReadModel(
            _readModelId,
            projectId,
            [
                new(_readModelProjectIdPropertyId, projectId),
                new(_readModelNamePropertyId, SemanticValue.Text("Screenplay"))
            ]);
        var success = new SemanticSpecification(
            Id(SemanticKind.Specification, "registers a project"),
            "registers a project",
            [],
            [],
            new(_commandId, commandValues),
            [new(_eventId, eventValues)],
            [readModelState],
            [new(_queryId, projectId, [readModelState])],
            []);
        var rejection = new SemanticSpecification(
            Id(SemanticKind.Specification, "rejects an empty name"),
            "rejects an empty name",
            [],
            [],
            new(
                _commandId,
                [
                    new(_commandProjectIdPropertyId, projectId),
                    new(_commandNamePropertyId, SemanticValue.Text(string.Empty))
                ]),
            [],
            [],
            [],
            [new(null, "Project name is required")]);
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
            _applicationIdentity,
            [],
            [new(Address(SemanticKind.Application, "Projects"), _applicationId, SemanticIdentityOrigin.Persisted)],
            [new(eventAddress, _eventContractId, EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)]);
    }

    protected SemanticApplication ReplaceSlice(SemanticSlice replacement) => ReplaceSlices(replacement);

    protected SemanticApplication ReplaceSlices(params SemanticSlice[] replacements)
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var replaced = feature.Slices.Select(slice => replacements.SingleOrDefault(_ => _.Id == slice.Id) ?? slice).ToImmutableArray();
        var replacedFeature = feature with { Slices = replaced };
        var replacedModule = module with { Features = [replacedFeature] };
        return _application with { Modules = [replacedModule] };
    }

    protected static SemanticAddress Address(SemanticKind kind, string key)
    {
        var application = ApplicationIdentity.Create("Projects");
        var stateChange = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var stateView = SemanticAddress.ForSlice(application, "Projects", "Projects", "ProjectSummaries");
        return kind switch
        {
            SemanticKind.Application => SemanticAddress.ForApplication(application),
            SemanticKind.Module => SemanticAddress.ForModule(application, key),
            SemanticKind.Feature => SemanticAddress.ForFeature(application, "Projects", key),
            SemanticKind.Slice => SemanticAddress.ForSlice(application, "Projects", "Projects", key),
            SemanticKind.Concept => SemanticAddress.ForConcept(application, key),
            SemanticKind.CompositeType => SemanticAddress.ForCompositeType(application, key),
            SemanticKind.Command => SemanticAddress.ForCommand(stateChange, key),
            SemanticKind.EventContract => SemanticAddress.ForEventContract(stateChange, key),
            SemanticKind.ReadModel => SemanticAddress.ForReadModel(stateView, key),
            SemanticKind.Projection => SemanticAddress.ForProjection(stateView, key),
            SemanticKind.Query => SemanticAddress.ForQuery(stateView, key),
            SemanticKind.Specification => SemanticAddress.ForSpecification(stateChange, key),
            SemanticKind.Property => PropertyAddress(application, stateChange, stateView, key),
            SemanticKind.QueryArgument => QueryArgumentAddress(stateView, key),
            _ => throw new InvalidSemanticContract($"Unsupported semantic kind '{kind}'.")
        };
    }

    protected static SemanticId Id(SemanticKind kind, string key) => SemanticId.Create(Address(kind, key));

    static SemanticAddress QueryArgumentAddress(SemanticAddress stateView, string key)
    {
        var separator = key.IndexOf('.');
        var owner = key[..separator];
        var member = key[(separator + 1)..];
        return SemanticAddress.ForQueryArgument(SemanticAddress.ForQuery(stateView, owner), member);
    }

    static SemanticAddress PropertyAddress(
        ApplicationIdentity application,
        SemanticAddress stateChange,
        SemanticAddress stateView,
        string key)
    {
        var separator = key.IndexOf('.');
        var owner = key[..separator];
        var member = key[(separator + 1)..];
        var ownerAddress = owner switch
        {
            "RegisterProject" => SemanticAddress.ForCommand(stateChange, owner),
            "ProjectRegistered" => SemanticAddress.ForEventContract(stateChange, owner),
            "ProjectSummary" => SemanticAddress.ForReadModel(stateView, owner),
            _ => SemanticAddress.ForCompositeType(application, owner)
        };
        return SemanticAddress.ForProperty(ownerAddress, member);
    }
}
#endif
