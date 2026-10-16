// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_describing_pending_command_handlers : Specification
{
    JsonArray _probes;
    JsonArray _diagnostics;

    void Because()
    {
        _probes = ContractAdmission.Create(["command"], ScreenplayContract.SupportedVersions())[0]["probes"].AsArray();
        _diagnostics = ContractDiagnostics.Create();
    }

    [Fact]
    void should_admit_the_described_body_only_from_v23()
    {
        var probe = _probes.Single(probe => probe["source"].GetValue<string>().Contains("Choose the payload", StringComparison.Ordinal));
        probe["minimumSemanticVersion"].GetValue<string>().ShouldEqual("23.0");
        probe["diagnostic"].ShouldBeNull();
        foreach (var entry in probe["admission"].AsArray())
        {
            entry["status"].GetValue<string>().ShouldEqual(entry["esmVersion"].GetValue<int>() >= 23 ? "admitted" : "refused");
        }
    }

    [Theory]
    [InlineData("returns value", DiagnosticCodes.DescribedCommandWithoutProductions)]
    [InlineData("produces when", DiagnosticCodes.DescribedCommandCardinalityNotAdmitted)]
    void should_publish_specific_binding_refusals(string form, string code)
    {
        var probe = _probes.Single(probe => probe["source"].GetValue<string>().Contains(form, StringComparison.Ordinal) && probe["source"].GetValue<string>().Contains("handler", StringComparison.Ordinal));
        probe["diagnostic"].GetValue<string>().ShouldEqual(code);
        probe["admission"].AsArray().All(entry => entry["status"].GetValue<string>() == "refused").ShouldBeTrue();
    }

    [Theory]
    [InlineData(DiagnosticCodes.DescribedCommandNotImplemented, "information")]
    [InlineData(DiagnosticCodes.DescribedCommandWithoutProductions, "error")]
    [InlineData(DiagnosticCodes.DescribedCommandCardinalityNotAdmitted, "error")]
    [InlineData(DiagnosticCodes.ReportOnlySemanticSyntax, "information")]
    [InlineData(DiagnosticCodes.TabIndentation, "warning")]
    void should_preserve_catalog_severities(string code, string severity)
    {
        var diagnostic = _diagnostics.Single(diagnostic => diagnostic["code"].GetValue<string>() == code);
        diagnostic["severity"].GetValue<string>().ShouldEqual(severity);
        diagnostic["severities"].AsArray().Select(value => value.GetValue<string>()).ShouldContainOnly(severity);
    }
}
