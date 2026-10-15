// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_resolving_same_named_query_references : given.a_model_with_same_named_queries
{
    [Theory]
    [InlineData("First.All", false)]
    [InlineData("Views.First.All", false)]
    [InlineData("Projects.Views.First.All", false)]
    [InlineData("First.All", true)]
    [InlineData("Views.First.All", true)]
    [InlineData("Projects.Views.First.All", true)]
    void should_resolve_a_qualified_reference(string reference, bool performed)
    {
        var result = Bind(WithReference(reference, performed));
        result.Diagnostics.ShouldBeEmpty();
        var slices = result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
        slices.Single(slice => slice.Name == "Third").Specifications.Single().ThenQueries.Single().Query
            .ShouldEqual(slices.Single(slice => slice.Name == "First").Queries.Single().Id);
    }

    [Fact] void should_not_let_a_local_query_override_a_qualified_reference()
    {
        var result = Bind(Source.Replace("then query All", "then query Projects.Views.Second.All", StringComparison.Ordinal)
            .Replace("id = \"first\"", "id = \"second\"", StringComparison.Ordinal));
        result.Diagnostics.ShouldBeEmpty();
        var slices = result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
        slices[0].Specifications.Single().ThenQueries.Single().Query.ShouldEqual(slices[^1].Queries.Single().Id);
    }

    [Fact] void should_resolve_an_unqualified_performed_query_in_its_slice()
    {
        var result = Bind(Source.Replace("then query All\n          result", "when query All\n        then result", StringComparison.Ordinal));
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.ShouldEachConformTo(slice =>
            slice.Specifications.Single().ThenQueries.Single().Query == slice.Queries.Single().Id);
    }

    [Fact] void should_prefer_the_nearest_feature_over_another_module()
    {
        var source = WithReference("All").Replace("    slice StateView Second", "module Other\n  feature Views\n    slice StateView Second", StringComparison.Ordinal);
        var result = Bind(source);
        result.Diagnostics.ShouldBeEmpty();
        var slices = result.Value!.Model.Application.Modules[^1].Features.Single().Slices;
        slices[^1].Specifications.Single().ThenQueries.Single().Query.ShouldEqual(slices[0].Queries.Single().Id);
    }

    [Fact] void should_keep_unique_unqualified_references_working()
    {
        var result = Bind(WithReference("Other").Replace("query All => FirstRow[]", "query Other => FirstRow[]", StringComparison.Ordinal)
            .Replace("then query All\n          result\n            id = \"first\"", "then query Other\n          result\n            id = \"first\"", StringComparison.Ordinal));
        result.Diagnostics.ShouldBeEmpty();
        var slices = result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
        slices[^1].Specifications.Single().ThenQueries.Single().Query.ShouldEqual(slices[0].Queries.Single().Id);
    }
}
