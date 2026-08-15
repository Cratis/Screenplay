// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_theme : given.a_compiler
{
    const string Source =
        """
        theme Aurora
          compatible with core
          compatible with PrimeReact
          compatible with Internal.Widgets

        theme Midnight
          compatible with core

        ui profile Desktop
          packages
            core
            PrimeReact
            Internal.Widgets

          theme Aurora
        """;

    CompilationResult<ApplicationSyntax> _result;
    ThemeSyntax _aurora;
    UiProfileSyntax _profile;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _aurora = _result.Value!.Themes!.First(theme => theme.Name == "Aurora");
        _profile = _result.Value!.UiProfiles!.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_both_themes() => _result.Value!.Themes!.Count().ShouldEqual(2);
    [Fact] void should_parse_auroras_compatible_packages() => _aurora.CompatibleWith.ShouldContainOnly("core", "PrimeReact", "Internal.Widgets");
    [Fact] void should_parse_the_profile_theme_reference() => _profile.Theme.ShouldEqual("Aurora");
}
