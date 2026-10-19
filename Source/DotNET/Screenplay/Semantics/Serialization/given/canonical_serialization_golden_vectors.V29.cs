// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV29Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v29.json");

    const string AcmeAdmissionsGoldenSource = """
        concept Money : Decimal
          validate
            >= 0
            decimals <= 2
        concept OrderId : Uuid
        concept MovementId : Uuid
        module Projects
          feature F
            slice StateChange Price
              event Priced
                amount Money
                movementId MovementId
              command SetPrice
                id String identifier
                orderId OrderId
                amount Money
                surcharge Decimal
                validate
                  surcharge decimals <= 3
                produces Priced
                  for id
                  amount = amount
                  movementId = orderId
            slice StateView Totals
              readmodel Basket
                id String key
                total Money?
              query GetBasket => Basket optional
                by id String
              projection Build => Basket
                from Priced
                  add total by amount * 2
        """;

    public static ExecutableSemanticModel CreateSemanticModelV29()
    {
        var source = AcmeAdmissionsGoldenSource;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Projects", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!.Model;
    }
}
#endif
