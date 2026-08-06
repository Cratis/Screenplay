// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_reporting_a_diagnostic;

public class and_two_constructs_hit_the_same_condition : given.a_compiler
{
    const string Source =
        """
        type Address
          street String
          city

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                number String
                issued
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_both_lines_under_one_code() => Malformed().Count.ShouldEqual(2);
    [Fact] void should_report_the_property_of_the_type() => Malformed().Exists(diagnostic => diagnostic.Location.Line == 3).ShouldBeTrue();
    [Fact] void should_report_the_property_of_the_event() => Malformed().Exists(diagnostic => diagnostic.Location.Line == 10).ShouldBeTrue();

    List<Diagnostic> Malformed() => [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPropertyDeclaration)];
}
