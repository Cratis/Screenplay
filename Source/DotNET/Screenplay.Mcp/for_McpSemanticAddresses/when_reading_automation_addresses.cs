// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticAddresses;

public class when_reading_automation_addresses : Specification
{
    SemanticAddress[] _expected = [];
    SemanticAddress[] _actual = [];

    void Because()
    {
        var app = ApplicationIdentity.Create("Billing");
        var slice = SemanticAddress.ForSlice(app, "Billing", "Payments", "Imports");
        var trigger = SemanticAddress.ForTrigger(app, "PaymentFileArrived");
        _expected =
        [
            trigger,
            SemanticAddress.ForProperty(trigger, "amount"),
            SemanticAddress.ForReaction(slice, "Importer"),
            SemanticAddress.ForCapture(slice, "LegacyPayments")
        ];
        _actual = [.. _expected.Select(address =>
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(McpSemanticAddresses.Describe(address), McpJson.Options);
            using var document = JsonDocument.Parse(bytes);
            return McpSemanticAddresses.Read(document.RootElement);
        })];
    }

    [Fact] void should_read_back_every_trigger_reaction_and_capture_address() => _actual.ShouldEqual(_expected);
}
