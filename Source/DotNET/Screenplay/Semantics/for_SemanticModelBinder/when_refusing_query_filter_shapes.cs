// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_query_filter_shapes : given.a_semantic_binder
{
    [Theory]
    [InlineData("Item", "String", "String", "by itemId String", "collection")]
    [InlineData("Item optional", "String", "String", "by itemId String", "collection")]
    [InlineData("Item[]", "String", "String", "by status String", "'by'")]
    [InlineData("Item[]", "String[]", "String", "", "scalar")]
    [InlineData("Item[]", "Details", "Details", "", "scalar")]
    void should_name_the_query_filter_and_reason(string resultType, string propertyType, string filterType, string by, string reason)
    {
        var source = $"type Details\n  value String\nmodule Items\n  feature Browse\n    slice StateView List\n      readmodel Item\n        itemId String\n        status {propertyType}\n      query Items => {resultType}\n        {by}\n        filter status {filterType}\n";
        var result = Bind(source);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax &&
            diagnostic.Message.Contains("Query 'Items' filter 'status'", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.Ordinal));
    }
}
