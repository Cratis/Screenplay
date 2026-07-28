// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.given;

public class a_printer : Specification
{
    protected ScreenplayCompiler _compiler;
    protected ScreenplayPrinter _printer;

    void Establish()
    {
        _compiler = new();
        _printer = new();
    }

    protected RoundTripResult RoundTrip(string source)
    {
        var original = _compiler.Compile(source);
        var printed = _printer.Print(original.Value!);
        var reparsed = _compiler.Compile(printed);
        return new(original, printed, reparsed, _printer.Print(reparsed.Value!));
    }

    protected RoundTripResult RoundTrip(ApplicationSyntax application)
    {
        var printed = _printer.Print(application);
        var reparsed = _compiler.Compile(printed);
        return new(null, printed, reparsed, _printer.Print(reparsed.Value!));
    }

    public record RoundTripResult(
        CompilationResult<ApplicationSyntax>? Original,
        string Printed,
        CompilationResult<ApplicationSyntax> Reparsed,
        string PrintedAgain);
}
