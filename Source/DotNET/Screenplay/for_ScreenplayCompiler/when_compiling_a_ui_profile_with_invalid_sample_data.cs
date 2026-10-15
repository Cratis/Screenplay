// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_ui_profile_with_invalid_sample_data : given.a_compiler
{
    const string Source =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Accounts
                name String

        sample data NordicDemo
          shape Accounts
            row
              name = "x"

        sample data NordicDemo

        ui profile Desktop
          sample data
            active Missing
            active NordicDemo
            shape Customer maps to Nothing
            shape Customer maps to Nothing
            bind Nothing to NordicDemo
            bind Accounts to Absent
            bind Accounts to NordicDemo
            something else
          sample data
            active NordicDemo
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    int Count(string code) => _result.Diagnostics.Count(diagnostic => diagnostic.Code == code);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_pack_declared_twice() => Count(DiagnosticCodes.DuplicateSampleDataPack).ShouldEqual(1);
    [Fact] void should_report_each_pack_the_document_does_not_declare() => Count(DiagnosticCodes.UnknownSampleDataPack).ShouldEqual(2);
    [Fact] void should_report_a_read_model_the_document_does_not_declare() => Count(DiagnosticCodes.UnknownSampleDataReadModel).ShouldEqual(2);
    [Fact] void should_report_a_repeated_active_pack_mapping_binding_and_block() => Count(DiagnosticCodes.DuplicateSampleDataSelection).ShouldEqual(4);
    [Fact] void should_report_a_line_that_is_not_a_selection() => Count(DiagnosticCodes.InvalidSampleDataSelection).ShouldEqual(1);
}
