// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_Users;

public class when_a_user_is_not_valid : Specification
{
    const string Source =
        """
        persona Clerk

        user 9bad as Clerk
        user kari as Clerk
          goal "Nested"
        user ola as Missing
        user ola as Clerk
        """;

    string[] _codes;

    void Because() => _codes = [.. new ScreenplayCompiler().Compile(Source).Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_report_the_malformed_and_nested_declarations() => _codes.Count(code => code == DiagnosticCodes.InvalidUserDeclaration).ShouldEqual(2);
    [Fact] void should_report_the_unknown_persona() => _codes.ShouldContain(DiagnosticCodes.UnknownUserPersona);
    [Fact] void should_report_the_repeated_name() => _codes.ShouldContain(DiagnosticCodes.DuplicateUser);
}
