// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_composite_key_lookups : given.a_compiler
{
    const string Source = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        resourceId String key\n        period Int key\n      query Find => Row optional\n        by\n          resourceId String\n          period Int\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_leave_legacy_fixture_identifier_validation_to_the_binder(bool includesAnExplicitKey)
    {
        var source = Source.Replace(" String key", " String", StringComparison.Ordinal).Replace(" Int key", " Int", StringComparison.Ordinal)
            .Replace("        by\n          resourceId String\n          period Int", "        by resourceId String", StringComparison.Ordinal) +
            "      specification Existing\n        given readmodel Row\n          period = 1\n        when query Find\n          resourceId = \"r\"\n        then no result\n";
        if (includesAnExplicitKey) source += "      readmodel Other\n        id String key\n";
        _compiler.Compile(source).Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_accept_a_complete_query() => _compiler.Compile(Source).Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_refuse_a_partial_query() => _compiler.Compile(Source.Replace("        by\n          resourceId String\n          period Int", "        by resourceId String", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey).ShouldBeTrue();

    [Fact]
    void should_allow_collection_queries_to_filter_on_a_subset() => _compiler.Compile(Source.Replace("Row optional", "Row[]", StringComparison.Ordinal).Replace("        by\n          resourceId String\n          period Int", "        by resourceId String", StringComparison.Ordinal)).Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_refuse_a_partial_screen_lookup() => _compiler.Compile(Source + "      screen Details\n        data Row via query Find by resourceId").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey).ShouldBeTrue();

    [Fact]
    void should_refuse_partial_absence_keys() => _compiler.Compile(Source + "      specification Missing\n        then no readmodel Row for {\"resourceId\":\"r\"}").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey).ShouldBeTrue();

    [Fact]
    void should_require_every_part_in_given_readmodel() => _compiler.Compile(Source + "      specification Existing\n        given readmodel Row\n          resourceId = \"r\"\n        when query Find\n          resourceId = \"r\"\n          period = 1\n        then no result").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationReadModelIdentifier && diagnostic.Message.Contains("period", StringComparison.Ordinal)).ShouldBeTrue();
}
