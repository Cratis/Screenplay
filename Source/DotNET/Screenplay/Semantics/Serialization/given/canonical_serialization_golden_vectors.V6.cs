// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    const string V6Resource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v6.json";

    public static byte[] SemanticModelV6Bytes => ReadResource(V6Resource);

    // The v5 model plus an application trigger, an automation slice whose reactions cover every trigger kind, and a
    // translate slice whose capture covers every map operation, condition, child collection and nested record - with
    // specifications that state a clock, advance it, fire triggers and present capture records.
    public static ExecutableSemanticModel CreateSemanticModelV6()
    {
        var v5 = CreateSemanticModelV5();
        var applicationIdentity = ApplicationIdentity.Create("Canonical Golden Application");
        var uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var wholeNumber = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber);
        var dateTime = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime);
        var occurred = new SemanticEventContextExpression(SemanticEventContextValueKind.Occurred, dateTime);
        const string batch = "5d9c0e2a-3b1f-4c7e-9a8d-6f5e4d3c2b1a";

        var trigger = new SemanticApplicationTrigger(Id(7000), "BatchArrived", [new(Id(7001), "batchId", uuid, false), new(Id(7002), "size", wholeNumber, false)]);
        var batchReceived = Event(applicationIdentity, Id(7110), "BatchReceived", [new(Id(7111), "batchId", uuid, false), new(Id(7112), "size", wholeNumber, false), new(Id(7113), "receivedAt", dateTime, false)]);
        var tickRecorded = Event(applicationIdentity, Id(7120), "TickRecorded", [new(Id(7121), "at", dateTime, false)]);
        var batchAcknowledged = Event(applicationIdentity, Id(7140), "BatchAcknowledged", [new(Id(7141), "batchId", uuid, false)]);
        var acknowledge = new SemanticCommand(
            Id(7130),
            "AcknowledgeBatch",
            [new(Id(7131), "batchId", uuid, true)],
            [],
            [new(batchAcknowledged.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(7131)), [new(Id(7141), SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(7131)))])])
        {
            Destination = new(uuid, SemanticExpression.Property(SemanticExpressionRootKind.Command, Id(7131)))
        };
        var handler = new SemanticReaction(Id(7150), "BatchHandler",
        [
            new(SemanticReactionTriggerKind.ApplicationTrigger)
            {
                Source = trigger.Id,
                Where = new SemanticComparison(new(Id(7002), null), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Number(0))),
                Produces =
                [
                    new(batchReceived.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Trigger, Id(7001)),
                    [
                        new(Id(7111), SemanticExpression.Property(SemanticExpressionRootKind.Trigger, Id(7001))),
                        new(Id(7112), SemanticExpression.Property(SemanticExpressionRootKind.Trigger, Id(7002))),
                        new(Id(7113), occurred)
                    ])
                    { Tags = ["automation"], DestinationType = uuid }
                ]
            },
            new(SemanticReactionTriggerKind.Event)
            {
                Source = batchReceived.Id,
                Invokes = [new(acknowledge.Id, [new(Id(7131), SemanticExpression.Property(SemanticExpressionRootKind.Event, Id(7111)))])]
            },
            new(SemanticReactionTriggerKind.Startup) { RequirementId = "reaction-startup" }
        ]);
        var clock = new SemanticReaction(Id(7160), "Clock",
        [
            new(SemanticReactionTriggerKind.Interval)
            {
                Every = 900,
                Produces = [new(tickRecorded.Id, null, SemanticExpression.FromValue(SemanticValue.Text("ticks")), [new(Id(7121), occurred)]) { DestinationType = text }]
            },
            new(SemanticReactionTriggerKind.Schedule)
            {
                At = 27_000,
                OnDayOfWeek = 1,
                Produces = [new(tickRecorded.Id, null, SemanticExpression.FromValue(SemanticValue.Text("weekly")), [new(Id(7121), occurred)]) { DestinationType = text }]
            },
            new(SemanticReactionTriggerKind.Schedule) { At = 0, OnDayOfMonth = 1, RequirementId = "reaction-monthly" },
            new(SemanticReactionTriggerKind.Shutdown)
        ]);
        var automation = new SemanticSlice(Id(7100), "Automations", SemanticSliceKind.Automation, [batchReceived, tickRecorded, batchAcknowledged], [acknowledge], [], [], [],
        [
            Specification(Id(7170), "fires a batch") with
            {
                GivenClock = "2026-10-02T09:00:00.0000000Z",
                WhenTrigger = new(SemanticReactionTriggerKind.ApplicationTrigger, [new(Id(7001), SemanticValue.Text(batch)), new(Id(7002), SemanticValue.Number(3))]) { Trigger = trigger.Id },
                ThenEvents =
                [
                    new(batchReceived.Id, [new(Id(7111), SemanticValue.Text(batch)), new(Id(7112), SemanticValue.Number(3)), new(Id(7113), SemanticValue.Text("2026-10-02T09:00:00.0000000Z"))])
                    { EventSource = new(uuid, SemanticValue.Text(batch)) },
                    new(batchAcknowledged.Id, [new(Id(7141), SemanticValue.Text(batch))]) { EventSource = new(uuid, SemanticValue.Text(batch)) }
                ]
            },
            Specification(Id(7171), "ticks the clock") with
            {
                GivenClock = "2026-10-05T07:00:00.0000000Z",
                WhenClock = "2026-10-05T07:30:00.0000000Z",
                ThenEvents =
                [
                    new(tickRecorded.Id, [new(Id(7121), SemanticValue.Text("2026-10-05T07:15:00.0000000Z"))]) { EventSource = new(text, SemanticValue.Text("ticks")) },
                    new(tickRecorded.Id, [new(Id(7121), SemanticValue.Text("2026-10-05T07:30:00.0000000Z"))]) { EventSource = new(text, SemanticValue.Text("weekly")) },
                    new(tickRecorded.Id, [new(Id(7121), SemanticValue.Text("2026-10-05T07:30:00.0000000Z"))]) { EventSource = new(text, SemanticValue.Text("ticks")) }
                ]
            },
            Specification(Id(7172), "starts up") with
            {
                WhenTrigger = new(SemanticReactionTriggerKind.Startup, []),
                ThenEvents = [new(tickRecorded.Id, [new(Id(7121), SemanticValue.Text("2026-10-05T07:30:00.0000000Z"))])]
            }
        ])
        {
            Reactions = [handler, clock]
        };

        var statusChanged = Event(applicationIdentity, Id(7220), "LegacyStatusChanged", [new(Id(7221), "status", text, false), new(Id(7222), "at", dateTime, false)]);
        var lineChanged = Event(applicationIdentity, Id(7230), "LegacyLineChanged", [new(Id(7231), "lineNumber", wholeNumber, false)]);
        var contactChanged = Event(applicationIdentity, Id(7240), "LegacyContactChanged", [new(Id(7241), "email", text, false)]);
        var capture = new SemanticCapture(
            Id(7210),
            "LegacyCapture",
            "id",
            [
                new(SemanticCaptureMapKind.Value, ["status"]) { Source = "status", Translations = [new("sendt", "sent"), new("betalt", "paid")] },
                new(SemanticCaptureMapKind.Template, ["summary"]) { Template = [new(null, "status"), new(" invoice", null)], Translations = [new("paid invoice", "settled")] },
                new(SemanticCaptureMapKind.Split, ["lastName", "firstName"]) { Source = "name", Separator = "," }
            ],
            [
                new(statusChanged.Id, text, new(SemanticCaptureConditionKind.AnyChanged, ["status"]), [new(Id(7221)) { Field = "status" }, new(Id(7222)) { Value = occurred }]) { Tags = ["legacy"] },
                new(statusChanged.Id, text, new(SemanticCaptureConditionKind.Transition, ["status"]) { From = "sent", To = "paid" }, [new(Id(7221)) { Value = SemanticExpression.FromValue(SemanticValue.Text("paid")) }, new(Id(7222)) { Value = occurred }]),
                new(statusChanged.Id, text, new(SemanticCaptureConditionKind.AllChanged, ["status", "summary"]), [new(Id(7221)) { Field = "summary" }, new(Id(7222)) { Value = occurred }]),
                new(statusChanged.Id, text, new(SemanticCaptureConditionKind.Expression, []) { Expression = "`status == \"paid\" && firstName != null`" }, [new(Id(7221)) { Field = "firstName" }, new(Id(7222)) { Value = occurred }]),
                new(statusChanged.Id, text, null, [new(Id(7221)) { Field = "lastName" }, new(Id(7222)) { Value = occurred }])
            ])
        {
            Children =
            [
                new("lineItems", "lineNumber", [new(SemanticCaptureMapKind.Value, ["product"]) { Source = "name" }],
                [
                    new(lineChanged.Id, text, new(SemanticCaptureConditionKind.Added, []), [new(Id(7231)) { Field = "lineNumber" }]),
                    new(lineChanged.Id, text, new(SemanticCaptureConditionKind.Removed, []), [new(Id(7231)) { Field = "lineNumber" }])
                ])
            ],
            Nested = [new("contact", [], [new(contactChanged.Id, text, new(SemanticCaptureConditionKind.AnyChanged, ["email"]), [new(Id(7241)) { Field = "email" }])])]
        };
        var translate = new SemanticSlice(Id(7200), "LegacySync", SemanticSliceKind.Translate, [statusChanged, lineChanged, contactChanged], [], [], [], [],
        [
            Specification(Id(7250), "captures a legacy record") with
            {
                GivenClock = "2026-10-02T12:00:00.0000000Z",
                GivenCaptures = [new(capture.Id, Record("sendt", [1, 2], "ada@example.com"))],
                WhenCapture = new(capture.Id, Record("betalt", [1, 3], "ada@lovelace.org")),
                ThenEvents = [new(contactChanged.Id, [new(Id(7241), SemanticValue.Text("ada@lovelace.org"))]) { EventSource = new(text, SemanticValue.Text("c-1")) }],
                ThenEventsInAnyOrder = true
            }
        ])
        {
            Reactions = [new(Id(7260), "Settler", [new(SemanticReactionTriggerKind.Event) { Source = statusChanged.Id }])],
            Captures = [capture]
        };

        var modules = v5.Application.Modules.Select((module, index) => index == 0
            ? module with { Features = module.Features.Add(new SemanticFeature(Id(7050), "Automation", [], [automation, translate])) }
            : module).ToImmutableArray();
        return ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, v5.Application with { Modules = modules, Triggers = [trigger] });
    }

    static SemanticEventContract Event(ApplicationIdentity application, SemanticId id, string name, ImmutableArray<SemanticProperty> properties) =>
        new(id, EventContractId.CreateLegacy(application, name), EventContractRevision.Initial, name, properties);

    static SemanticSpecification Specification(SemanticId id, string name) => new(id, name, [], [], null, [], [], [], []);

    static SemanticCaptureRecord Record(string status, int[] lines, string email) => new(
    [
        new("id", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text("c-1") },
        new("status", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text(status) },
        new("name", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text("Lovelace, Ada") },
        new("archived", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Boolean(false) },
        new("note", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Null },
        new("lineItems", SemanticCaptureFieldKind.Records)
        {
            Records = [.. lines.Select(line => new SemanticCaptureRecord(
            [
                new("lineNumber", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Number(line) },
                new("name", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text($"Item {line}") }
            ]))]
        },
        new("contact", SemanticCaptureFieldKind.Record) { Record = new([new("email", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text(email) }]) }
    ]);
}
#endif
