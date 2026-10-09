// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_classifying_reserved_codes : Specification
{
    [Fact]
    void should_publish_the_reservation_and_retirement_from_the_owning_catalog()
    {
        var diagnostics = JsonNode.Parse(ScreenplayContract.Serialize())["diagnostics"].AsArray();
        foreach (var field in typeof(DiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var reservation = field.GetCustomAttribute<DiagnosticReservationAttribute>();
            var entry = diagnostics.Single(entry => entry["code"].GetValue<string>() == (string)field.GetRawConstantValue());
            entry["reserved"].GetValue<bool>().ShouldEqual(reservation is not null);
            entry["retired"].GetValue<bool>().ShouldEqual(reservation?.Retired ?? false);
            if (reservation is not null) entry["severities"].AsArray().Select(value => value.GetValue<string>()).ShouldEqual([reservation.Severity.ToString().ToLowerInvariant()]);
        }
    }
}
