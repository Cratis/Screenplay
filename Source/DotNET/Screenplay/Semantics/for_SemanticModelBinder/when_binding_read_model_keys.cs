// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_read_model_keys : given.a_semantic_binder
{
    const string Source = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        rowId String\n        period Int\n      query Find => Row optional\n        by rowId String\n";

    [Fact]
    void should_bind_an_explicit_single_key_with_inferred_bytes()
    {
        var inferred = Bind(Source);
        var declared = Bind(Source.Replace("        rowId String\n", "        rowId String key\n", StringComparison.Ordinal));
        inferred.Success.ShouldBeTrue();
        declared.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(declared.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(inferred.Value!.Model));
    }

    [Fact]
    void should_resolve_ambiguity_with_an_explicit_key()
    {
        const string ambiguous = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        rowId String\n        otherId String\n      query Rows => Row[]\n";
        Bind(ambiguous).Success.ShouldBeFalse();
        var result = Bind(ambiguous.Replace("        rowId String\n", "        rowId String key\n", StringComparison.Ordinal));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_refuse_a_composite_key_with_the_admission_issue()
    {
        var source = Source.Replace("        rowId String\n", "        rowId String key\n", StringComparison.Ordinal).Replace("period Int\n", "period Int key\n", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("/599", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_each_fixture_using_a_composite_view()
    {
        var source = Source.Replace("        rowId String\n", "        rowId String key\n", StringComparison.Ordinal).Replace("period Int\n", "period Int key\n", StringComparison.Ordinal) +
            "      specification Missing\n        given readmodel Row\n          rowId = \"r\"\n          period = 1\n        then no readmodel Row for {\"rowId\":\"r\",\"period\":1}\n";
        var result = Bind(source);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("Specification read model", StringComparison.Ordinal) && diagnostic.Message.Contains("/599", StringComparison.Ordinal)).ShouldEqual(2);
    }

    [Fact]
    void should_refuse_a_by_block_query_with_the_admission_issue()
    {
        var result = Bind(Source.Replace("        by rowId String", "        by\n          rowId String\n          period Int", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("Query 'Find'", StringComparison.Ordinal) && diagnostic.Message.Contains("/599", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
