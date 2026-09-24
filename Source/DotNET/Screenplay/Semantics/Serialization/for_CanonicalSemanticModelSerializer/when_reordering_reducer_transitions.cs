// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reordering_reducer_transitions : Specification
{
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal
              event AmountWithdrawn
                amount Decimal
              readmodel AccountBalance
                id Uuid
              query BalanceById => AccountBalance?
                by id Uuid
              reducer BalanceReducer => AccountBalance
                on AmountDeposited
                  file Reducers/Deposited.cs
                on AmountWithdrawn
                  file Reducers/Withdrawn.cs
        """;

    byte[] _first = null!;
    byte[] _reversed = null!;

    void Because()
    {
        const string firstRules = "on AmountDeposited\n      file Reducers/Deposited.cs\n    on AmountWithdrawn\n      file Reducers/Withdrawn.cs";
        const string reverseRules = "on AmountWithdrawn\n      file Reducers/Withdrawn.cs\n    on AmountDeposited\n      file Reducers/Deposited.cs";
        _first = Serialize(Source);
        _reversed = Serialize(Source.Replace(firstRules, reverseRules, StringComparison.Ordinal));
    }

    [Fact] void should_preserve_canonical_bytes() => _reversed.SequenceEqual(_first).ShouldBeTrue();

    static byte[] Serialize(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("balance"), "balance", "Balance.play", source);
        var bound = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        if (bound.Value is null)
        {
            throw new InvalidSemanticContract(string.Join("; ", bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return SemanticModelSerializer.Serialize(bound.Value.Model);
    }
}
