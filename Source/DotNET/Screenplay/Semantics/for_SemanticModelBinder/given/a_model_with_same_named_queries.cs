// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public class a_model_with_same_named_queries : a_semantic_binder
{
    protected const string Source = """
        module Projects
          feature Views
            slice StateView First
              readmodel FirstRow
                id String key
              query All => FirstRow[]
              specification FirstRows
                given readmodel FirstRow
                  id = "first"
                then query All
                  result
                    id = "first"
            slice StateView Second
              readmodel SecondRow
                id String key
              query All => SecondRow[]
              specification SecondRows
                given readmodel SecondRow
                  id = "second"
                then query All
                  result
                    id = "second"
        """;

    protected static string WithReference(string reference, bool performed = false) => Source + "\n" + (performed
        ? $"""
            slice StateView Third
              specification OtherRows
                when query {reference}
                then no result
        """
        : $"""
            slice StateView Third
              specification OtherRows
                then query {reference}
        """);
}
