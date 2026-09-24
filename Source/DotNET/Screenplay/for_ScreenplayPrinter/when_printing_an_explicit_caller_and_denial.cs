// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_an_explicit_caller_and_denial : given.a_printer
{
    const string Source =
        """
        specification RejectingAnUnauthorizedAction
          given caller
            authenticated
            role "Auditor"
            claim "department" = "Engineering"
            claim "DEPARTMENT" = "Finance"
          when RegisterInvoice
            invoiceId = "99"
          then denied
        """;

    CompilationResult<SpecificationSyntax> _parsed;
    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        _parsed = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(_parsed.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_parse_without_errors() => _parsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_errors() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_retain_all_claim_values() => _reparsed.Value!.GivenCaller!.Claims.Count().ShouldEqual(2);
    [Fact] void should_retain_the_denied_assertion() => _reparsed.Value!.ThenDenied.ShouldNotBeNull();
    [Fact] void should_print_idempotently() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
}
