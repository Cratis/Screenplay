// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_writing_the_contract : Specification
{
    string _json;
    string _golden;
    JsonObject _contract;

    void Establish()
    {
        using var reader = new StreamReader(typeof(ScreenplayContract).Assembly.GetManifestResourceStream("screenplay-contract.json"));
        _golden = reader.ReadToEnd();
    }

    void Because()
    {
        using var writer = new StringWriter();
        ScreenplayContract.Write(writer);
        _json = writer.ToString();
        _contract = JsonNode.Parse(_json).AsObject();
    }

    [Fact] void should_match_the_checked_in_golden() => _json.ShouldEqual(_golden);
    [Fact] void should_be_deterministic() => ScreenplayContract.Serialize().ShouldEqual(_json);
    [Fact] void should_identify_the_schema_version() => _contract["schemaVersion"].GetValue<int>().ShouldEqual(1);
    [Fact] void should_classify_every_catalog_code() => _contract["diagnostics"].AsArray().Select(value => value["code"].GetValue<string>()).ShouldEqual(typeof(DiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral).Select(field => (string)field.GetRawConstantValue()).Order(StringComparer.Ordinal));
    [Fact] void should_have_a_title_and_severity_for_every_code() => _contract["diagnostics"].AsArray().All(value => !string.IsNullOrWhiteSpace(value["title"].GetValue<string>()) && value["severities"].AsArray().Count > 0 && Enum.TryParse<DiagnosticSeverity>(value["severity"].GetValue<string>(), true, out _)).ShouldBeTrue();
    [Fact] void should_preserve_the_retired_reads_code() => _contract["diagnostics"].AsArray().Single(value => value["code"].GetValue<string>() == DiagnosticCodes.DuplicateReads)["retired"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_include_the_new_specification_runner() => _contract["mcpTools"].AsArray().Any(value => value["name"].GetValue<string>() == "run-specifications").ShouldBeTrue();
    [Fact] void should_classify_each_construct_for_every_supported_version() => _contract["constructs"].AsArray().All(value => value["admission"].AsArray().Count == _contract["esmVersions"].AsArray().Count).ShouldBeTrue();
    [Fact] void should_include_conditions_only_for_conditional_entries() => _contract["constructs"].AsArray().SelectMany(value => value["admission"].AsArray()).All(value => value.AsObject().ContainsKey("condition") == (value["status"].GetValue<string>() == "conditional")).ShouldBeTrue();
}
