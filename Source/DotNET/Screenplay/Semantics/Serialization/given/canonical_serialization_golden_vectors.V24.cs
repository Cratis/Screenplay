// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV24Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v24.json");

    public static ExecutableSemanticModel CreateSemanticModelV24()
    {
        var application = CreateSemanticModelV23().Application;
        var rules = new[] { SemanticValidationRuleKind.Contains, SemanticValidationRuleKind.NotContains, SemanticValidationRuleKind.StartsWith, SemanticValidationRuleKind.EndsWith };
        var concept = new SemanticConcept(Id(124000), "TextReference", SemanticPrimitiveType.Text, [],
            [.. rules.Select(kind => new SemanticValidationRule(default, kind, SemanticValue.Text(kind == SemanticValidationRuleKind.NotContains ? " " : "A"), null))]);
        application = application with { Concepts = application.Concepts.Add(concept) };

        return ExecutableSemanticModel.Create(LanguageVersion.V24, SemanticVersion.V24, application, 2026);
    }
}
#endif
