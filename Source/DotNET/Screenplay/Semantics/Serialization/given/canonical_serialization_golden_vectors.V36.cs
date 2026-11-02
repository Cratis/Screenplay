// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV36Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v36.json");

    public static ExecutableSemanticModel CreateSemanticModelV36()
    {
        const string source = a_lifecycle_source.Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
