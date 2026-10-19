// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_has_an_invalid_identity : given.a_compiler
{
    const string Source =
        """
        ui profile Initials
          identity
            logo initials "ABC"

        ui profile Icon
          identity
            logo icon rocket

        ui profile Image
          identity
            logo image "https://example.com/logo.svg"

        ui profile Corners
          appearance
            corners pointy

        ui profile Fine
          identity
            name "Fine"
            logo initials "F"
          appearance
            corners default
            font inter
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    IEnumerable<Cratis.Screenplay.Diagnostics.Diagnostic> Refused => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAppearanceValue);

    [Fact] void should_refuse_the_long_initials_the_unknown_icon_the_remote_image_and_the_unknown_corners() => Refused.Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(3, 7, 11, 15);
    [Fact] void should_report_errors() => Refused.ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_name_the_initials() => Refused.First().Message.ShouldContain("'ABC'");
    [Fact] void should_name_the_icon() => Refused.Skip(1).First().Message.ShouldContain("'rocket'");
    [Fact] void should_not_refuse_a_valid_identity() => Refused.Count().ShouldEqual(4);
}
