// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_resolving_query_authorization : given.a_model_with_same_named_queries
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    void should_require_a_caller_for_the_resolved_query(bool performed, bool inherited)
    {
        var result = Bind(Authorized(WithReference("Second.All", performed), inherited));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.MissingSpecificationCaller);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_use_another_same_named_query_authorization(bool performed)
    {
        var result = Bind(Authorized(WithReference("First.All", performed)));
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact] void should_require_a_caller_for_a_qualified_reference_to_a_later_slice()
    {
        var source = Authorized(Source).Replace("then query All\n          result\n            id = \"first\"", "then query Second.All\n          result\n            id = \"first\"", StringComparison.Ordinal)
            .Replace("specification FirstRows\n        given caller\n          authenticated\n", "specification FirstRows\n", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.MissingSpecificationCaller);
    }

    [Fact] void should_resolve_local_authorization_independently()
    {
        var source = Authorized(Source).Replace("specification SecondRows\n        given caller\n          authenticated\n", "specification SecondRows\n", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.MissingSpecificationCaller);
    }

    static string Authorized(string source, bool inherited = false)
    {
        source = "policy Access\n  require authenticated\n" + source
            .Replace("specification FirstRows\n", "specification FirstRows\n        given caller\n          authenticated\n", StringComparison.Ordinal)
            .Replace("specification SecondRows\n", "specification SecondRows\n        given caller\n          authenticated\n", StringComparison.Ordinal);
        return inherited
            ? source.Replace("    slice StateView Second", "module Other\n  authorize Access\n  feature Views\n    slice StateView Second", StringComparison.Ordinal)
            : source.Replace("query All => SecondRow[]\n", "query All => SecondRow[]\n        authorize Access\n", StringComparison.Ordinal);
    }
}
