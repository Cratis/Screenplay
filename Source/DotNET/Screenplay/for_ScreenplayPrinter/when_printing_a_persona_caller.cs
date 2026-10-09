// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_persona_caller : given.a_printer
{
    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var parsed = _compiler.CompileSpecification("specification Denied\n  given caller as Accountant\n  when RegisterInvoice\n  then denied");
        _printed = _printer.Print(parsed.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_keep_the_bodyless_persona_form() => _printed.ShouldContain("given caller as Accountant");
    [Fact] void should_reparse() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_retain_the_persona_reference() => _reparsed.Value!.GivenCallerPersona!.Name.ShouldEqual("Accountant");
    [Fact] void should_print_idempotently() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
}
