// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_admission_probes : Specification
{
    public static TheoryData<string> Probes
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var probe in ContractAdmission.Probes) data.Add(probe.Keyword);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Probes))]
    void should_publish_the_actual_binding_result_for_every_probe_and_supported_pair(string keyword)
    {
        var definition = ContractAdmission.Probes.Single(probe => probe.Keyword == keyword);
        var versions = ScreenplayContract.SupportedVersions();
        var contract = ContractAdmission.Create([keyword], versions)[0];
        var cases = contract["probes"].AsArray();
        for (var index = 0; index < cases.Count; index++)
        {
            var probe = cases[index];

            // Bind independently rather than calling the production observation/classification routine.
            var compilation = ContractAdmission.Bind(probe["source"].GetValue<string>(), index == 0 ? definition.Imported : null);
            var baselineCompilation = index == 0 && definition.Baseline is not null ? ContractAdmission.Bind(definition.Baseline) : null;
            var disposition = compilation.Diagnostics.FirstOrDefault(diagnostic => baselineCompilation?.Diagnostics.Contains(diagnostic) != true && (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax || diagnostic.Code == DiagnosticCodes.DeferredSemanticSyntax));
            probe["diagnostic"]?.GetValue<string>().ShouldEqual(disposition?.Code);
            var diagnostics = compilation.Diagnostics.Where(diagnostic => baselineCompilation?.Diagnostics.Contains(diagnostic) != true).ToArray();
            probe["diagnostics"].AsArray().Select(diagnostic => diagnostic["code"].GetValue<string>()).ShouldEqual(diagnostics.Select(diagnostic => diagnostic.Code));
            probe["diagnostics"].AsArray().Select(diagnostic => diagnostic["severity"].GetValue<string>()).ShouldEqual(diagnostics.Select(diagnostic => diagnostic.Severity.ToString().ToLowerInvariant()));
            probe["minimumSemanticVersion"]?.GetValue<string>().ShouldEqual(compilation.Value?.Model.SemanticVersion.ToString());
            var metadataOnly = baselineCompilation is not null && compilation.Value?.Model.Revision == baselineCompilation.Value?.Model.Revision;
            foreach (var entry in probe["admission"].AsArray())
            {
                var support = versions.Single(version => version["schemaVersion"].GetValue<int>() == entry["esmVersion"].GetValue<int>());
                var supported = !metadataOnly && disposition is null && compilation.Value is { } value && support["supportedPairs"].AsArray().Any(pair => SemanticVersion.Parse(pair["semanticVersion"].GetValue<string>()).IsAtLeast(value.Model.SemanticVersion));
                entry["status"].GetValue<string>().ShouldEqual(supported ? "admitted" : "refused");
                entry["diagnostic"]?.GetValue<string>().ShouldEqual(disposition?.Code);
            }
        }
        foreach (var entry in contract["admission"].AsArray())
        {
            var version = entry["esmVersion"].GetValue<int>();
            var baseline = cases[0]["admission"].AsArray().Single(item => item["esmVersion"].GetValue<int>() == version);
            var refused = cases.Select((probe, index) => (probe, index)).Where(pair => pair.probe["admission"].AsArray().Single(item => item["esmVersion"].GetValue<int>() == version)["status"].GetValue<string>() == "refused").ToArray();
            var conditional = baseline["status"].GetValue<string>() == "admitted" && refused.Length > 0;
            entry["status"].GetValue<string>().ShouldEqual(conditional ? "conditional" : baseline["status"].GetValue<string>());
            entry["diagnostic"]?.GetValue<string>().ShouldEqual(conditional ? refused.Select(pair => pair.probe["diagnostic"]?.GetValue<string>()).FirstOrDefault(code => code is not null) : baseline["diagnostic"]?.GetValue<string>());
            if (conditional) entry["condition"].AsArray().Select(value => value.GetValue<int>()).ShouldEqual(refused.Select(pair => pair.index));
        }
    }
}
