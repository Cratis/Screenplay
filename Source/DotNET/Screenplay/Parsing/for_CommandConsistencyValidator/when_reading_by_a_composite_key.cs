// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_CommandConsistencyValidator;

public class when_reading_by_a_composite_key : Specification
{
    readonly ScreenplayCompiler _compiler = new();
    const string Source = "module M\n  feature F\n    slice StateChange S\n      readmodel Row\n        resourceId String key\n        period Int key\n      command C\n        resourceId String\n        month Int\n";

    [Fact]
    void should_accept_a_complete_key() => _compiler.Compile(Source + "        reads Row\n          by\n            period = month\n            resourceId = resourceId").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_name_the_missing_parts_of_a_partial_single_by()
    {
        var diagnostic = _compiler.Compile(Source + "        reads Row by resourceId").Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey);
        diagnostic.Message.ShouldContain("period");
    }

    [Theory]
    [InlineData("unknown = month\n            resourceId = resourceId")]
    [InlineData("period = resourceId\n            resourceId = resourceId")]
    [InlineData("period = 1\n            resourceId = resourceId")]
    [InlineData("period = month\n            resourceId = missing")]
    void should_refuse_unknown_incompatible_and_literal_sources(string mappings) => _compiler.Compile(Source + "        reads Row\n          by\n            " + mappings).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();

    [Fact]
    void should_refuse_a_block_on_a_single_key_view() => _compiler.Compile(Source.Replace("period Int key", "period Int", StringComparison.Ordinal) + "        reads Row\n          by\n            period = month\n            resourceId = resourceId").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();

    [Fact]
    void should_refuse_paths_through_optional_records()
    {
        var result = _compiler.Compile("type Input\n  period Int\n" + Source + "        input Input optional\n        reads Row\n          by\n            period = input.period\n            resourceId = resourceId");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();
    }

    [Fact]
    void should_require_selected_application_trigger_values()
    {
        var result = _compiler.Compile("trigger Kick\n  resourceId String\n  period Int\n" + Source.Replace("slice StateChange", "slice Automation", StringComparison.Ordinal) + "      reaction R\n        when Kick\n          resourceId\n          reads Row\n            by\n              resourceId = resourceId\n              period = period");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();
    }

    [Fact]
    void should_allow_the_trigger_event_data()
    {
        var result = _compiler.Compile(Source.Replace("slice StateChange", "slice Automation", StringComparison.Ordinal) + "      event Kick\n        resourceId String\n        period Int\n      reaction R\n        when Kick\n          resourceId\n          reads Row\n            by\n              resourceId = resourceId\n              period = period");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Int optional")]
    [InlineData("Int[]")]
    void should_refuse_optional_and_collection_sources(string type) => _compiler.Compile(Source.Replace("month Int", "month " + type, StringComparison.Ordinal) + "        reads Row\n          by\n            period = month\n            resourceId = resourceId").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();
}
