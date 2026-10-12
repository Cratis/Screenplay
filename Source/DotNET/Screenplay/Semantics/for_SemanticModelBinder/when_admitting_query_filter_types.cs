// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_query_filter_types : given.a_semantic_binder
{
    [Theory]
    [InlineData("String", "String", "")]
    [InlineData("String optional", "String", "")]
    [InlineData("String", "String optional", "")]
    [InlineData("Status", "Status optional", "concept Status : String")]
    [InlineData("Status", "Status", "concept Status : String\n  values open closed")]
    void should_admit_nominal_scalar_types_ignoring_optionality(string propertyType, string filterType, string declarations)
    {
        var source = $"{declarations}\nmodule Items\n  feature Browse\n    slice StateView List\n      readmodel Item\n        itemId String\n        status {propertyType}\n      query Items => Item[]\n        filter status {filterType}\n";
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }
}
