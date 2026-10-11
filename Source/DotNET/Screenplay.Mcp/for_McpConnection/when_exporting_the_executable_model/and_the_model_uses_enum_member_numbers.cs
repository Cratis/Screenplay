// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_the_model_uses_enum_member_numbers : given.an_export
{
    JsonElement _first;
    JsonElement _concept;
    bool _aligned;
    bool _roundTrips;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept Status : Enum\n  draft = -1\n  issued = 4\n");
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

    [Fact] void should_select_v12() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(12);
    [Fact] void should_round_trip_exported_bytes() => _roundTrips.ShouldBeTrue();
    [Fact] void should_align_page_offsets() => _aligned.ShouldBeTrue();
    [Fact] void should_export_numbers_parallel_to_names() => _concept.GetProperty("numbers").EnumerateArray().Select(number => number.GetInt32()).ShouldContainOnly(-1, 4);
    [Fact] void should_keep_names() => _concept.GetProperty("values").EnumerateArray().Select(name => name.GetString()).ShouldContainOnly("draft", "issued");
}
