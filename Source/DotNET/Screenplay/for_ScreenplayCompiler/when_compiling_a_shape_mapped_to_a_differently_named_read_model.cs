// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_shape_mapped_to_a_differently_named_read_model : given.a_compiler
{
    const string Mapped =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Accounts
                name String

        sample data NordicDemo
          shape Customer
            row
              name = "x"
              nickname = "y"

        ui profile Desktop
          sample data
            shape Customer maps to Accounts
        """;

    const string Unmapped =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Accounts
                name String

        sample data NordicDemo
          shape Customer
            row
              name = "x"
        """;

    CompilationResult<ApplicationSyntax> _mapped;
    CompilationResult<ApplicationSyntax> _unmapped;

    void Because()
    {
        _mapped = _compiler.Compile(Mapped);
        _unmapped = _compiler.Compile(Unmapped);
    }

    [Fact] void should_check_the_rows_against_the_mapped_read_model() => _mapped.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownSampleDataField);
    [Fact] void should_not_warn_about_a_mapped_shape() => _mapped.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSampleDataShape).ShouldBeFalse();
    [Fact] void should_warn_about_an_unmapped_shape() => _unmapped.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownSampleDataShape);
    [Fact] void should_still_succeed_for_an_unmapped_shape() => _unmapped.Success.ShouldBeTrue();
}
