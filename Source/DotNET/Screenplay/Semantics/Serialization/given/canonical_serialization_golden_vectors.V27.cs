// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.for_SemanticModelBinder;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV27Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v27.json");

    public static ExecutableSemanticModel CreateSemanticModelV27()
    {
        const string source = when_binding_composite_read_model_keys.Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeKeys"));
        const string key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelCompiler().Compile("CompositeKeys", SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!.Model;
    }
}
#endif
