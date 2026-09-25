// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_the_model_uses_v4 : an_export
{
    JsonElement _first;
    byte[] _bytes = [];
    bool _roundTrips;

    void Because()
    {
        SetupVersion(4);
        (_first, _bytes, _) = ExportPages();
        _roundTrips = StrictRoundTrip(_bytes);
    }

    [Fact] void should_export_the_canonical_v4_bytes() => _roundTrips.ShouldBeTrue();
    [Fact] void should_label_the_export_as_v4() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(4);
}
