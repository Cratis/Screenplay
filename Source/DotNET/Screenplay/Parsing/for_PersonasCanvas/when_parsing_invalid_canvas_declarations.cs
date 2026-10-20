// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PersonasCanvas;

public class when_parsing_invalid_canvas_declarations : Specification
{
    const string Source =
        """
        persona Shopper
          goal Find things
          position 10
          size -5 20
          position 1 2
          position 3 4
          shape round

        sticky note on personas
          colour purple
          description "No position"

        sticky note somewhere
          position 0 0

        eventsource Product
          position 1
          position 1 2
          position 3 4
        """;

    ScreenplayCompiler _compiler;
    string[] _codes;

    void Establish() => _compiler = new();

    void Because() => _codes = [.. _compiler.Compile(Source).Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_report_the_invalid_goal_position_and_size() => _codes.Count(code => code == DiagnosticCodes.InvalidPersonaDeclaration).ShouldEqual(9);
    [Fact] void should_report_the_unknown_persona_directive() => _codes.ShouldContain(DiagnosticCodes.UnknownPersonaDirective);
}
