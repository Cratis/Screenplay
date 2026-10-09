// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_schema_labels_differ_from_semantic_versions : Specification
{
    JsonArray _admission;

    void Because()
    {
        var versions = JsonNode.Parse("""
            [
              {"schemaVersion":99,"supportedPairs":[{"languageVersion":"1.0","semanticVersion":"1.0"}]},
              {"schemaVersion":1,"supportedPairs":[{"languageVersion":"6.0","semanticVersion":"6.0"}]}
            ]
            """)!.AsArray();
        _admission = ContractAdmission.Create(["trigger"], versions)[0]!["admission"]!.AsArray();
    }

    [Fact] void should_refuse_a_high_schema_label_that_lacks_the_semantics() => _admission[0]!["status"]!.GetValue<string>().ShouldEqual("refused");
    [Fact] void should_admit_a_low_schema_label_that_supports_the_semantics() => _admission[1]!["status"]!.GetValue<string>().ShouldEqual("admitted");
}
