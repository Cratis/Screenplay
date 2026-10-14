// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV21Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v21.json");

    public static ExecutableSemanticModel CreateSemanticModelV21()
    {
        var application = CreateSemanticModelV20().Application;
        SemanticConcept[] concepts =
        [
            new(Id(121000), "PatternReference", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text("luhn"), null) { Severity = SemanticValidationSeverity.Warning }])
                { Generation = new("pattern") { Pattern = "[AB]{1,2}", Checksum = "luhn" } },
            new(Id(121001), "TicketNumber", SemanticPrimitiveType.WholeNumber, [], [])
                { Generation = new("random") { Minimum = long.MinValue, Maximum = long.MaxValue } },
            new(Id(121002), "TenantOrderId", SemanticPrimitiveType.Text, [], [])
                { Generation = new("implementation") { RequirementId = new string('a', 64) } }
        ];

        return ExecutableSemanticModel.Create(LanguageVersion.V21, SemanticVersion.V21, application with { Concepts = application.Concepts.AddRange(concepts) }, 2026);
    }
}
#endif
