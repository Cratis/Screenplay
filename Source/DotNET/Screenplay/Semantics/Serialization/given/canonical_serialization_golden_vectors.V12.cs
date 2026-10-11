// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV12Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v12.json");

    public static ExecutableSemanticModel CreateSemanticModelV12()
    {
        var application = CreateSemanticModelV11().Application;
        var status = new SemanticConcept(Id(10200), "InvoiceStatus", SemanticPrimitiveType.Text, ["draft", "issued", "cancelled"], []) { Numbers = [-1, 4, int.MaxValue] };
        application = application with { Concepts = application.Concepts.Add(status) };

        return ExecutableSemanticModel.Create(LanguageVersion.V12, SemanticVersion.V12, application);
    }
}
#endif
