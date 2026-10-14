// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV20Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v20.json");

    public static ExecutableSemanticModel CreateSemanticModelV20()
    {
        var application = CreateSemanticModelV19().Application;
        var concept = application.Concepts.Single(value => value.Name == "Generation5");
        concept = concept with
        {
            Validations =
            [
                new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^[A-Za-z0-9_-]{21}$"), null),
                new(default, SemanticValidationRuleKind.Length, SemanticValue.Number(21), null)
            ]
        };
        application = application with { Concepts = application.Concepts.SetItem(application.Concepts.IndexOf(application.Concepts.Single(value => value.Id == concept.Id)), concept) };

        return ExecutableSemanticModel.Create(LanguageVersion.V20, SemanticVersion.V20, application);
    }
}
#endif
