// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV36_1Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v36.1.json");

    public static ExecutableSemanticModel CreateSemanticModelV36_1()
    {
        var result = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true));
        if (!result.Success) throw new InvalidSemanticContract(result.Refusal?.Reason ?? string.Join('\n', result.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Compilation.Value!.Model;
    }
}
#endif
