// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_sample_data_with_invalid_rows : given.a_compiler
{
    const string Source =
        """
        concept Status : Enum
          active
          lead

        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                name String
                revenue Decimal
                age Int
                active Bool
                status Status
                notes String optional

        sample data Broken
          title "One"
          title "Two"
          locale nb_NO
          shape Customer
            row
              nickname = "x"
              revenue = "lots"
              age = 4.5
              active = "yes"
              name = null
              status = "Churned"
              notes = 5
              name = "again"
              broken line
              extra = nope
          shape Customer
          shape Ghost
            row
              a = 1
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    int Count(string code) => _result.Diagnostics.Count(diagnostic => diagnostic.Code == code);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_repeated_title() => Count(DiagnosticCodes.UnknownSampleDataDirective).ShouldEqual(1);
    [Fact] void should_report_an_invalid_locale() => Count(DiagnosticCodes.InvalidSampleDataLocale).ShouldEqual(1);
    [Fact] void should_report_a_field_that_is_not_a_property_of_the_read_model() => Count(DiagnosticCodes.UnknownSampleDataField).ShouldEqual(1);
    [Fact] void should_report_each_type_mismatch() => Count(DiagnosticCodes.SampleDataTypeMismatch).ShouldEqual(6);
    [Fact] void should_report_a_repeated_field() => Count(DiagnosticCodes.DuplicateSampleDataField).ShouldEqual(1);
    [Fact] void should_report_a_line_that_is_not_a_field() => Count(DiagnosticCodes.InvalidSampleDataValue).ShouldEqual(2);
    [Fact] void should_report_a_repeated_shape() => Count(DiagnosticCodes.DuplicateSampleDataShape).ShouldEqual(1);
    [Fact] void should_warn_about_a_shape_naming_no_read_model() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSampleDataShape).Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
