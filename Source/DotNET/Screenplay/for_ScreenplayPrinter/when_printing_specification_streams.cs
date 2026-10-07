// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_specification_streams : given.a_printer
{
    const string Source = """
        specification Routed
          given E
            stream = 5
            stream Account.Transactions
              streamId = "p-1:2026-10"
            for "other"
          when append E
            for "other"
            stream Account.Profile
          then E
            no stream
        """;

    string _printed;
    string _printedAgain;

    void Because()
    {
        _printed = _printer.Print(_compiler.CompileSpecification(Source).Value);
        _printedAgain = _printer.Print(_compiler.CompileSpecification(_printed).Value);
    }

    [Fact] void should_round_trip() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_print_route_after_identity_before_payload() => _printed.ShouldContain("    for \"other\"\n    stream Account.Transactions\n      streamId = \"p-1:2026-10\"\n    stream = 5");
    [Fact] void should_preserve_no_stream() => _printed.ShouldContain("    no stream");
}
