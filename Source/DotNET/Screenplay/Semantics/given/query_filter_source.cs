// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.given;

public static class query_filter_source
{
    public const string Source =
        """
        module Items
          feature Browsing
            slice StateView Browse
              readmodel Item
                itemId String
                status String
                category String
              query Filtered => Item[]
                filter status String
                filter category String optional
              query InCategory => Item[]
                by category String
                filter status String
              specification ReversedRows
                given readmodel Item
                  itemId = "a"
                  status = "open"
                  category = "x"
                given readmodel Item
                  itemId = "b"
                  status = "open"
                  category = "y"
                given readmodel Item
                  itemId = "c"
                  status = "closed"
                  category = "x"
                when query Filtered
                  status = "open"
                then result
                  itemId = "b"
                then result
                  itemId = "a"
              specification DistinctCalls
                given readmodel Item
                  itemId = "a"
                  status = "open"
                  category = "x"
                given readmodel Item
                  itemId = "c"
                  status = "closed"
                  category = "x"
                then query Filtered
                  arguments
                    status = "open"
                  result
                    itemId = "a"
                then query Filtered
                  arguments
                    status = "closed"
                  result
                    itemId = "c"
        """;

    public static SemanticCompilation Compile(string? source = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("QueryFilters"));
        const string key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source ?? Source);
        var result = new SemanticModelCompiler().Compile("QueryFilters", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));

        return result.Value!;
    }
}
