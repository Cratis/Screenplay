// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static class canonical_serialization_golden_vectors
{
    const string SemanticModelResource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v1.json";
    const string ExpressionResource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-expressions-v1.json";
    const string IdentityCatalogResource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-identity-catalog-v1.json";

    public static byte[] SemanticModelBytes => ReadResource(SemanticModelResource);
    public static byte[] ExpressionBytes => ReadResource(ExpressionResource);
    public static byte[] IdentityCatalogBytes => ReadResource(IdentityCatalogResource);

    public static ExecutableSemanticModel CreateSemanticModel()
    {
        var applicationIdentity = ApplicationIdentity.Create("Canonical Golden Application");
        var uuidConcept = Id(10);
        var textConcept = Id(11);
        var wholeNumberConcept = Id(12);
        var decimalNumberConcept = Id(13);
        var booleanConcept = Id(14);
        var dateConcept = Id(15);
        var dateTimeConcept = Id(16);
        var detailsType = Id(20);
        var envelopeType = Id(21);
        var createdEvent = Id(50);
        var optionalEvent = Id(51);
        var manyEvent = Id(52);
        var command = Id(60);
        var readModel = Id(70);
        var projection = Id(80);
        var byId = Id(90);
        var maybeById = Id(91);
        var byLabel = Id(92);

        var commandId = Id(200);
        var commandTitle = Id(201);
        var commandEnabled = Id(202);
        var commandAmount = Id(203);
        var commandNote = Id(204);
        var commandDestination = Id(205);
        var eventId = Id(210);
        var eventLabel = Id(211);
        var eventAmount = Id(212);
        var eventEnabled = Id(213);
        var eventNote = Id(214);
        var eventCode = Id(215);
        var optionalEventId = Id(220);
        var manyEventIds = Id(230);
        var readModelId = Id(240);
        var readModelLabel = Id(241);
        var readModelNote = Id(242);

        var concepts = ImmutableArray.Create(
            new SemanticConcept(dateTimeConcept, "OccurredAt", SemanticPrimitiveType.DateTime, [], []),
            new SemanticConcept(uuidConcept, "EntityId", SemanticPrimitiveType.Uuid, [], []),
            new SemanticConcept(booleanConcept, "Enabled", SemanticPrimitiveType.Boolean, [], []),
            new SemanticConcept(
                decimalNumberConcept,
                "Amount",
                SemanticPrimitiveType.DecimalNumber,
                [],
                [
                    new(default, SemanticValidationRuleKind.Minimum, SemanticValue.Number(-999.5000m), "Amount is too small"),
                    new(default, SemanticValidationRuleKind.Maximum, SemanticValue.Number(999.5000m), "Amount is too large")
                ]),
            new SemanticConcept(
                textConcept,
                "Label",
                SemanticPrimitiveType.Text,
                ["second", "café", "first", "accepted", "rejected", "^[a-z]+$", "created", "screenplay"],
                [
                    new(default, SemanticValidationRuleKind.NotEmpty, null, "Label is required"),
                    new(default, SemanticValidationRuleKind.Equal, SemanticValue.Text("accepted"), null),
                    new(default, SemanticValidationRuleKind.NotEqual, SemanticValue.Text("rejected"), null),
                    new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^[a-z]+$"), "Use lowercase letters")
                ]),
            new SemanticConcept(dateConcept, "EffectiveDate", SemanticPrimitiveType.Date, [], []),
            new SemanticConcept(wholeNumberConcept, "Count", SemanticPrimitiveType.WholeNumber, [], []));

        var types = ImmutableArray.Create(
            new SemanticCompositeType(
                envelopeType,
                "Envelope",
                [new(Id(123), "Enabled", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean), false)]),
            new SemanticCompositeType(
                detailsType,
                "Details",
                [
                    new(Id(122), "Envelope", SemanticTypeReference.ForCompositeType(envelopeType, isOptional: true), false),
                    new(Id(120), "Count", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false),
                    new(Id(121), "Label", SemanticTypeReference.ForConcept(textConcept, isCollection: true), false)
                ]));

        var createdContract = new SemanticEventContract(
            createdEvent,
            EventContractId.CreateLegacy(applicationIdentity, "EntityCreated"),
            EventContractRevision.Initial,
            "EntityCreated",
            [
                new(eventNote, "Note", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true), false),
                new(eventId, "Id", SemanticTypeReference.ForConcept(uuidConcept), true),
                new(eventCode, "Code", SemanticTypeReference.ForConcept(textConcept), false),
                new(eventEnabled, "Enabled", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean), false),
                new(eventAmount, "Amount", SemanticTypeReference.ForConcept(decimalNumberConcept), false),
                new(eventLabel, "Label", SemanticTypeReference.ForConcept(textConcept), false)
            ]);
        var optionalContract = new SemanticEventContract(
            optionalEvent,
            EventContractId.CreateLegacy(applicationIdentity, "EntityMaybeSelected"),
            EventContractRevision.Initial,
            "EntityMaybeSelected",
            [new(optionalEventId, "Id", SemanticTypeReference.ForConcept(uuidConcept, isOptional: true), false)]);
        var manyContract = new SemanticEventContract(
            manyEvent,
            EventContractId.CreateLegacy(applicationIdentity, "EntitiesSelected"),
            EventContractRevision.Initial,
            "EntitiesSelected",
            [new(manyEventIds, "Ids", SemanticTypeReference.ForConcept(uuidConcept, isCollection: true), false)]);

        var createCommand = new SemanticCommand(
            command,
            "CreateEntity",
            [
                new(commandNote, "Note", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true), false),
                new(commandId, "Id", SemanticTypeReference.ForConcept(uuidConcept), true),
                new(commandDestination, "Destination", SemanticTypeReference.ForConcept(uuidConcept), false),
                new(commandAmount, "Amount", SemanticTypeReference.ForConcept(decimalNumberConcept), false),
                new(commandTitle, "Title", SemanticTypeReference.ForConcept(textConcept), false),
                new(commandEnabled, "Enabled", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean), false)
            ],
            [new(commandTitle, SemanticValidationRuleKind.NotEmpty, null, "Title is required")],
            [new(
                createdEvent,
                SemanticExpression.Property(SemanticExpressionRootKind.Command, commandEnabled),
                SemanticExpression.Property(SemanticExpressionRootKind.Command, commandDestination),
                [
                    new(eventLabel, SemanticExpression.Property(SemanticExpressionRootKind.Command, commandTitle)),
                    new(eventId, SemanticExpression.Property(SemanticExpressionRootKind.Command, commandId)),
                    new(eventCode, SemanticExpression.FromValue(SemanticValue.Text("created"))),
                    new(eventAmount, SemanticExpression.FromValue(SemanticValue.Number(123.4500m))),
                    new(eventEnabled, SemanticExpression.FromValue(SemanticValue.Boolean(true))),
                    new(eventNote, SemanticExpression.FromValue(SemanticValue.Null))
                ]),
                new(optionalEvent, null, null, [])]);

        var entitySummary = new SemanticReadModel(
            readModel,
            "EntitySummary",
            [
                new(readModelNote, "Note", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true), false),
                new(readModelLabel, "Label", SemanticTypeReference.ForConcept(textConcept), false),
                new(readModelId, "Id", SemanticTypeReference.ForConcept(uuidConcept), true)
            ]);
        var entityProjection = new SemanticProjection(
            projection,
            "EntitySummaryProjection",
            readModel,
            [
                new(
                    optionalEvent,
                    new(AffectedInstanceCardinality.ZeroOrOne, SemanticExpression.Property(SemanticExpressionRootKind.Event, optionalEventId)),
                    []),
                new(
                    createdEvent,
                    new(AffectedInstanceCardinality.One, SemanticExpression.Property(SemanticExpressionRootKind.Event, eventId)),
                    [
                        new(readModelLabel, SemanticExpression.Property(SemanticExpressionRootKind.Event, eventLabel)),
                        new(readModelId, SemanticExpression.Property(SemanticExpressionRootKind.Event, eventId)),
                        new(readModelNote, SemanticExpression.FromValue(SemanticValue.Null))
                    ]),
                new(
                    manyEvent,
                    new(AffectedInstanceCardinality.Many, SemanticExpression.Property(SemanticExpressionRootKind.Event, manyEventIds)),
                    [])
            ]);

        var queries = ImmutableArray.Create(
            new SemanticKeyedQuery(
                byLabel,
                "EntitiesByLabel",
                new(Id(112), "label", SemanticTypeReference.ForConcept(textConcept)),
                readModel,
                readModelLabel,
                SemanticQueryCardinality.Many,
                SemanticQueryDelivery.Snapshot),
            new SemanticKeyedQuery(
                byId,
                "EntityById",
                new(Id(110), "id", SemanticTypeReference.ForConcept(uuidConcept)),
                readModel,
                readModelId,
                SemanticQueryCardinality.One,
                SemanticQueryDelivery.Snapshot),
            new SemanticKeyedQuery(
                maybeById,
                "MaybeEntityById",
                new(Id(111), "id", SemanticTypeReference.ForConcept(uuidConcept)),
                readModel,
                readModelId,
                SemanticQueryCardinality.ZeroOrOne,
                SemanticQueryDelivery.Live));

        var idValue = SemanticValue.Text("00000000-0000-0000-0000-000000000123");
        var titleValue = SemanticValue.Text("screenplay");
        var commandValues = ImmutableArray.Create(
            new SemanticPropertyValue(commandEnabled, SemanticValue.Boolean(true)),
            new SemanticPropertyValue(commandNote, SemanticValue.Null),
            new SemanticPropertyValue(commandDestination, idValue),
            new SemanticPropertyValue(commandAmount, SemanticValue.Number(123.4500m)),
            new SemanticPropertyValue(commandTitle, titleValue),
            new SemanticPropertyValue(commandId, idValue));
        var eventValues = ImmutableArray.Create(
            new SemanticPropertyValue(eventEnabled, SemanticValue.Boolean(true)),
            new SemanticPropertyValue(eventNote, SemanticValue.Null),
            new SemanticPropertyValue(eventCode, SemanticValue.Text("created")),
            new SemanticPropertyValue(eventAmount, SemanticValue.Number(123.4500m)),
            new SemanticPropertyValue(eventLabel, titleValue),
            new SemanticPropertyValue(eventId, idValue));
        var readModelState = new SemanticSpecificationReadModel(
            readModel,
            idValue,
            [
                new(readModelNote, SemanticValue.Null),
                new(readModelLabel, titleValue),
                new(readModelId, idValue)
            ]);
        var success = new SemanticSpecification(
            Id(95),
            "creates an entity from existing state",
            [new(createdEvent, eventValues)],
            [readModelState],
            new(command, commandValues),
            [new(createdEvent, eventValues)],
            [readModelState],
            [
                new(byLabel, titleValue, [readModelState]),
                new(maybeById, idValue, []),
                new(byId, idValue, [readModelState])
            ],
            []);
        var rejection = new SemanticSpecification(
            Id(96),
            "rejects an invalid title",
            [],
            [],
            new(command, commandValues),
            [],
            [],
            [],
            [new("title.required", "Title is required")]);
        var bareRejection = rejection with
        {
            Id = Id(97),
            Name = "rejects without details",
            ThenErrors = [new(null, null)]
        };
        var messageOnlyRejection = rejection with
        {
            Id = Id(98),
            Name = "rejects with only a message",
            ThenErrors = [new(null, "Title is invalid")]
        };

        var stateChange = new SemanticSlice(
            Id(40),
            "Creation",
            SemanticSliceKind.StateChange,
            [manyContract, createdContract, optionalContract],
            [createCommand],
            [],
            [],
            [],
            [messageOnlyRejection, rejection, success, bareRejection]);
        var stateView = new SemanticSlice(
            Id(41),
            "EntitySummaries",
            SemanticSliceKind.StateView,
            [],
            [],
            [entitySummary],
            [entityProjection],
            queries,
            []);
        var nestedFeature = new SemanticFeature(Id(32), "Nested", [], [stateView, stateChange]);
        var application = new SemanticApplication(
            Id(1),
            "Canonical Golden Application",
            concepts,
            types,
            [new(Id(30), "Operations", [new(Id(31), "Entities", [nestedFeature], [])])]);

        return ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
    }

    public static ImmutableArray<SemanticExpression> CreateExpressions() =>
    [
        SemanticExpression.FromValue(SemanticValue.Null),
        SemanticExpression.FromValue(SemanticValue.Text("Café")),
        SemanticExpression.FromValue(SemanticValue.Number(1.2300m)),
        SemanticExpression.FromValue(SemanticValue.Boolean(true)),
        SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(1)),
        SemanticExpression.Property(SemanticExpressionRootKind.Event, Id(2)),
        SemanticExpression.Argument(SemanticExpressionRootKind.Query, Id(3))
    ];

    public static ExecutableSemanticModel CreateSemanticModelWithDecimal(decimal value)
    {
        var model = CreateSemanticModel();
        var concepts = model.Application.Concepts
            .Select(concept => concept.Name == "Amount"
                ? concept with
                {
                    Validations = [new(default, SemanticValidationRuleKind.Equal, SemanticValue.Number(value), "Exact amount")]
                }
                : concept)
            .ToImmutableArray();
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Concepts = concepts });
    }

    public static ExecutableSemanticModel CreateNestedFeatureModel(int featureLevels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(featureLevels, 1);

        SemanticFeature feature = new(Id(1000 + featureLevels), $"Level{featureLevels}", [], []);
        for (var level = featureLevels - 1; level >= 1; level--)
        {
            feature = new(Id(1000 + level), $"Level{level}", [feature], []);
        }

        var application = new SemanticApplication(
            Id(900),
            "Depth Boundary",
            [],
            [],
            [new(Id(901), "Depth", [feature])]);
        return ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
    }

    public static SemanticIdentityCatalog CreateIdentityCatalog()
    {
        var application = ApplicationIdentity.Create("Canonical Golden Identity Catalog");
        var module = SemanticAddress.ForModule(application, "Billing");
        var feature = SemanticAddress.ForFeature(application, "Billing", "Invoices");
        var nestedFeature = SemanticAddress.ForFeature(application, "Billing", ["Invoices", "Approvals"]);
        var slice = SemanticAddress.ForSlice(application, "Billing", ["Invoices", "Approvals"], "Review");
        var compositeType = SemanticAddress.ForCompositeType(application, "InvoiceDetails");
        var command = SemanticAddress.ForCommand(slice, "ApproveInvoice");
        var eventContract = SemanticAddress.ForEventContract(slice, "InvoiceApproved");
        var secondEventContract = SemanticAddress.ForEventContract(slice, "InvoiceRejected");
        var readModel = SemanticAddress.ForReadModel(slice, "InvoiceStatus");
        var query = SemanticAddress.ForQuery(slice, "InvoiceById");
        var queryArgument = SemanticAddress.ForQueryArgument(query, "invoiceId");
        var addresses = new[]
        {
            SemanticAddress.ForProperty(readModel, "Status"),
            SemanticAddress.ForSpecification(slice, "approves an invoice"),
            SemanticAddress.ForApplication(application),
            eventContract,
            SemanticAddress.ForProperty(compositeType, "Amount"),
            query,
            nestedFeature,
            SemanticAddress.ForConcept(application, "InvoiceId"),
            SemanticAddress.ForProperty(command, "InvoiceId"),
            module,
            SemanticAddress.ForProjection(slice, "InvoiceStatusProjection"),
            SemanticAddress.ForProperty(eventContract, "InvoiceId"),
            readModel,
            SemanticAddress.ForCompositeType(application, "InvoiceDetails"),
            command,
            slice,
            feature,
            queryArgument
        };
        var semantics = addresses
            .Select((address, index) => new SemanticIdentityAssignment(
                address,
                SemanticId.Create(address),
                index % 2 == 0 ? SemanticIdentityOrigin.Persisted : SemanticIdentityOrigin.LegacyBootstrap))
            .ToImmutableArray();
        var documents = ImmutableArray.Create(
            new DocumentIdentityAssignment("z-café.play", DocumentId.Create("z-café.play"), SemanticIdentityOrigin.Persisted),
            new DocumentIdentityAssignment("a-first.play", DocumentId.Create("a-first.play"), SemanticIdentityOrigin.LegacyBootstrap));
        var eventContracts = ImmutableArray.Create(
            new EventContractIdentityAssignment(
                secondEventContract,
                EventContractId.CreateLegacy(application, secondEventContract.Name),
                EventContractRevision.Initial,
                SemanticIdentityOrigin.Persisted),
            new EventContractIdentityAssignment(
                eventContract,
                EventContractId.CreateLegacy(application, eventContract.Name),
                EventContractRevision.Initial,
                SemanticIdentityOrigin.LegacyBootstrap));
        return SemanticIdentityCatalog.Create(application, documents, semantics, eventContracts);
    }

    static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");

    static byte[] ReadResource(string name)
    {
        using var stream = typeof(canonical_serialization_golden_vectors).Assembly.GetManifestResourceStream(name) ??
            throw new InvalidOperationException($"Embedded golden vector '{name}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
#endif
