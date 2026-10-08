// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_no_event_expectations : given.a_printer
{
    SpecificationSyntax _restored;
    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var parsed = _compiler.CompileSpecification("specification NothingHappens\n  when DoNothing\n  then no events");
        _restored = (SpecificationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        _printed = _printer.Print(_restored);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_restore_the_explicit_assertion_from_json() => _restored.ThenNoEvents.ShouldBeTrue();
    [Fact] void should_print_the_assertion() => _printed.ShouldContain("then no events");
    [Fact] void should_reparse() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_retain_the_assertion() => _reparsed.Value!.ThenNoEvents.ShouldBeTrue();
    [Fact] void should_print_stably() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
}
