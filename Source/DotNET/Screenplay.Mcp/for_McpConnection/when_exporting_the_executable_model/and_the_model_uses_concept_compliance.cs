// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_the_model_uses_concept_compliance : given.an_export
{
    JsonElement _first;
    JsonElement _concept;
    bool _aligned;
    bool _roundTrips;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept HealthInformation : String pii secret\n  pii special health\n  pii criminal\n  pii reason \"A note\"\n");
        Initialize();
    }

    void Because()
    {
        byte[] bytes;
        (_first, bytes, _aligned) = ExportPages();
        _roundTrips = StrictRoundTrip(bytes);
        using var document = JsonDocument.Parse(bytes);
        _concept = document.RootElement.GetProperty("application").GetProperty("concepts")[0].Clone();
    }

    [Fact] void should_select_v13() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(13);
    [Fact] void should_round_trip_exported_bytes() => _roundTrips.ShouldBeTrue();
    [Fact] void should_align_page_offsets() => _aligned.ShouldBeTrue();
    [Fact] void should_export_personal_qualifiers() => _concept.GetProperty("compliance").GetProperty("personal").GetProperty("special").GetString().ShouldEqual("health");
    [Fact] void should_export_criminal_data() => _concept.GetProperty("compliance").GetProperty("personal").GetProperty("criminal").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_secret_marker_without_scope() => _concept.GetProperty("compliance").GetProperty("secret").EnumerateObject().ShouldBeEmpty();
    [Fact] void should_omit_reasons() => _concept.GetProperty("compliance").GetProperty("personal").TryGetProperty("reason", out _).ShouldBeFalse();
}
