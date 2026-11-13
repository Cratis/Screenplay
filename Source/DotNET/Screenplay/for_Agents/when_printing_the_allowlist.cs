// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Agents;

public class when_printing_the_allowlist : given.an_agent_model
{
    string _printed;
    string _printedAgain;
    CompilationResult<ApplicationSyntax> _reparsed;

    void Because()
    {
        var source = Model + "\n" + Allowlist.Replace("Find invoices", "Find \\\"invoices\\\" at C:\\\\invoices\\nnow", StringComparison.Ordinal);
        var printer = new ScreenplayPrinter();
        _printed = printer.Print(Compiler.Compile(source).Value!);
        _reparsed = Compiler.Compile(_printed);
        _printedAgain = printer.Print(_reparsed.Value!);
    }

    [Fact] void should_recompile() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_be_idempotent() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_escaped_text() => _reparsed.Value!.Agents!.Operations.First().Description.ShouldEqual("Find \"invoices\" at C:\\invoices\nnow");
}
