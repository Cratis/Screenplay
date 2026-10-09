// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_PurposeParser;

public class when_parsing_processing_purposes : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    PurposeSyntax _purpose;
    ApplicationSyntax _roundTrip;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = compiler.Compile("""
            purpose Billing
              description "Issue invoices"
              basis legalObligation "Accounting law"
              subjects customer, customerContact
              retention "Five years"
              recipient "Tax authority"
              recipient "Accountant"
              transfer "United States" safeguard "Standard clauses"
              erasure exception legalObligation
            module Finance
              purpose Billing
              feature Invoices
                purpose Billing
                slice StateChange Issue
                  purpose Billing
            """);
        _purpose = _result.Value!.Purposes.Single();
        _roundTrip = compiler.Compile(new ScreenplayPrinter().Print(_result.Value)).Value!;
    }

    [Fact] void should_compile_without_findings() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_record_the_legal_reference() => _purpose.BasisReference.ShouldEqual("Accounting law");
    [Fact] void should_record_multiple_recipients() => _purpose.Recipients.ShouldContainOnly("Tax authority", "Accountant");
    [Fact] void should_record_the_transfer_safeguard() => _purpose.Transfers.Single().Safeguard.ShouldEqual("Standard clauses");
    [Fact] void should_preserve_syntax_on_printing() => SyntaxJson.StructurallyEqual(_result.Value!, _roundTrip).ShouldBeTrue();
}
