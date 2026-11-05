// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_sample_data_with_invalid_nested_rows : given.a_compiler
{
    const string Source = """
        type Line
          quantity Int

        module Shop
          feature Baskets
            slice StateView BasketList
              readmodel Basket
                name String
                tags String[]
                line Line
                lines Line[]

        sample data Demo
          shape Basket
            row
              name
                row
              tags
                row
              line
                row
              lines
                row
                  unknown = "value"
                  quantity = "two"
                  quantity = 2
                not a row
                  ignored = "value"
              lines
                row
              broken line
            row
              lines = "not rows"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_reject_rows_on_non_composite_collections_and_child_mismatches() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.SampleDataTypeMismatch).ShouldEqual(5);
    [Fact] void should_reject_an_unknown_child_field() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSampleDataField).ShouldEqual(1);
    [Fact] void should_reject_a_non_row_child() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSampleDataDirective).ShouldEqual(1);
    [Fact] void should_keep_rejecting_a_multi_word_field_line() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSampleDataValue).ShouldEqual(1);
    [Fact] void should_reject_a_duplicate_collection_field() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateSampleDataField).ShouldEqual(2);
    [Fact] void should_report_only_the_expected_diagnostics() => _result.Diagnostics.Count().ShouldEqual(10);
    [Fact] void should_keep_the_existing_scalar_on_collection_message() => _result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains("a row holds a single value", StringComparison.Ordinal)).Message.ShouldEqual("Field 'lines' of shape 'Basket' in sample data pack 'Demo' does not fit 'Line' on read model 'Basket' - a row holds a single value, not a collection");
}
