// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV31Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v31.json");

    const string AnonymousPolicyGoldenSource = """
        policy Anyone
          require anonymous
        module Catalog
          feature Browsing
            slice StateChange RegisterInterest
              command RegisterInterest
                authorize Anyone
                productId String identifier
                email String
                produces InterestRegistered
                  for productId
                  productId = productId
                  email = email
              event InterestRegistered
                productId String
                email String
              specification RegisteringInterestNeedsNoCaller
                when RegisterInterest
                  productId = "p1"
                  email = "ada@example.com"
                then InterestRegistered
                  productId = "p1"
                  email = "ada@example.com"
        """;

    public static ExecutableSemanticModel CreateSemanticModelV31()
    {
        var source = AnonymousPolicyGoldenSource;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Catalog"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Catalog", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!.Model;
    }
}
#endif
