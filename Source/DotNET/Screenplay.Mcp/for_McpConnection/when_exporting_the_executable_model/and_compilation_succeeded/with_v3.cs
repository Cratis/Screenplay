// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.and_compilation_succeeded;

public class with_v3 : given.an_export
{
    System.Text.Json.JsonElement _first;
    byte[] _bytes = null!;
    bool _aligned;

    void Establish() => SetupVersion(3);
    void Because() => (_first, _bytes, _aligned) = ExportPages();

    [Fact] void should_be_available() => _first.GetProperty("available").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_schema_version() => _first.GetProperty("schemaVersion").GetInt32().ShouldEqual(3);
    [Fact] void should_report_language_version() => _first.GetProperty("languageVersion").GetString().ShouldEqual("3.0");
    [Fact] void should_report_semantic_version() => _first.GetProperty("semanticVersion").GetString().ShouldEqual("3.0");
    [Fact] void should_page_exact_byte_count() => _bytes.Length.ShouldEqual(_first.GetProperty("totalBytes").GetInt32());
    [Fact] void should_align_each_byte_page() => _aligned.ShouldBeTrue();
    [Fact] void should_round_trip_strict_canonical_bytes() => StrictRoundTrip(_bytes).ShouldBeTrue();
}
