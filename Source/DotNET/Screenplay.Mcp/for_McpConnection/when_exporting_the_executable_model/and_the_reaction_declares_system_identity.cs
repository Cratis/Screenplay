// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_the_reaction_declares_system_identity : given.an_export
{
    JsonElement _first;
    JsonElement _model;
    bool _aligned;
    bool _roundTrips;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"),
            "policy Access\n  require role \"Automation\"\nmodule Work\n  feature F\n    slice Automation S\n      command Complete\n        authorize Access\n      reaction Worker\n        runs as system role \"Automation\"\n        when Startup\n          invokes Complete\n");
        Initialize();
    }

    void Because()
    {
        byte[] bytes;
        (_first, bytes, _aligned) = ExportPages();
        _roundTrips = StrictRoundTrip(bytes);
        using var document = JsonDocument.Parse(bytes);
        _model = document.RootElement.Clone();
    }

    [Fact] void should_select_v10() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(10);
    [Fact] void should_round_trip_exported_bytes() => _roundTrips.ShouldBeTrue();
    [Fact] void should_align_page_offsets() => _aligned.ShouldBeTrue();
    [Fact] void should_export_system_kind() => Identity().GetProperty("kind").GetString().ShouldEqual("system");
    [Fact] void should_export_exact_roles() => Identity().GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ShouldContainOnly("Automation");

    JsonElement Identity() => _model.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("reactions")[0].GetProperty("runsAs");
}
