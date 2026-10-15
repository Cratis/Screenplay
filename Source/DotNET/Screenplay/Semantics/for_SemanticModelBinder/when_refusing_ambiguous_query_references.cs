// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_ambiguous_query_references : given.a_model_with_same_named_queries
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_report_ambiguity_at_the_reference_instead_of_the_declarations(bool performed)
    {
        var source = WithReference("All", performed);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.AmbiguousReference);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual("Ambiguous query 'All' - it matches 2 declarations equally well (Projects.Views.First, Projects.Views.Second); qualify it to say which");
        diagnostic.Location.Line.ShouldEqual(Array.FindLastIndex(source.Split('\n'), line => line.Contains("query All", StringComparison.Ordinal)) + 1);
    }

    [Theory]
    [InlineData("Missing.All")]
    [InlineData("Other.Views.First.All")]
    void should_not_discard_invalid_qualification(string reference)
    {
        var result = Bind(WithReference(reference));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
        result.Diagnostics.Single().Message.ShouldEqual($"Specification query '{reference}' is unresolved.");
    }

    [Fact] void should_refuse_qualified_suffixes_that_match_multiple_modules()
    {
        var source = WithReference("Views.First.All").Replace("    slice StateView Second", "module Other\n  feature Views\n    slice StateView First", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AmbiguousReference);
    }
}
