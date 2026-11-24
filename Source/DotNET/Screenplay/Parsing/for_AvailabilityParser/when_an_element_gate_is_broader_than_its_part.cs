// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_an_element_gate_is_broader_than_its_part : Specification
{
    static readonly string Header =
        """
        audience Plus

        audience Pro

        module Fleet
          feature Quality
            slice StateChange Register
              event VesselRegistered
                name String

              command RegisterVessel
                name String
                available for Plus
                produces VesselRegistered
                  name = name

        """;

    CompilationResult<ApplicationSyntax> _broader;
    CompilationResult<ApplicationSyntax> _narrower;
    CompilationResult<ApplicationSyntax> _equal;
    CompilationResult<ApplicationSyntax> _ungated;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _broader = compiler.Compile(Header + Screen("available for Plus or Pro"));
        _narrower = compiler.Compile(Header + Screen("available for Plus and Pro"));
        _equal = compiler.Compile(Header + Screen("available for Plus"));
        _ungated = compiler.Compile(Header + Screen(null));
    }

    static string Screen(string? gate) =>
        "      screen RegisterScreen\n        action RegisterVessel\n" + (gate is null ? string.Empty : $"          {gate}\n");

    [Fact] void should_warn_when_the_gate_is_broader() => Codes(_broader).ShouldContain(DiagnosticCodes.ElementGateBroaderThanPart);
    [Fact] void should_stay_silent_when_the_gate_narrows() => Codes(_narrower).ShouldNotContain(DiagnosticCodes.ElementGateBroaderThanPart);
    [Fact] void should_stay_silent_when_the_gate_equals_the_part() => Codes(_equal).ShouldNotContain(DiagnosticCodes.ElementGateBroaderThanPart);
    [Fact] void should_stay_silent_when_the_element_has_no_gate_of_its_own() => Codes(_ungated).ShouldNotContain(DiagnosticCodes.ElementGateBroaderThanPart);

    static IEnumerable<string> Codes(CompilationResult<ApplicationSyntax> result) => result.Diagnostics.Select(diagnostic => diagnostic.Code);
}
