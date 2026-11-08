// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_DesignSystem;

public class when_resolving_a_theme : given.a_compiler
{
    const string Source =
        given.a_design_system_document.Source + """


        theme Dusk
          implements D
          palette
            ink   = "#0B1020"
            white = "#FFFFFF"
            teal  = "#0F766E"
          values
            radius.control = "2px"
          light
            color.surface    = palette.white
            color.text       = palette.ink
            color.primary    = palette.teal
            color.on-primary = palette.white
          dark
            color.surface    = palette.ink
            color.text       = palette.white
            color.primary    = color.text
            color.on-primary = palette.ink

        ui profile Floor
          theme Dusk
          appearance
            color scheme light
        """;

    ApplicationSyntax _application;
    ResolvedDesignTheme _dark;
    ResolvedDesignTheme _light;
    ResolvedDesignTheme _dusk;
    ResolvedDesignTheme _floor;
    ResolvedDesignTheme _office;

    void Because()
    {
        _application = _compiler.Compile(Source).Value!;
        var theme = _application.Themes!.First(candidate => candidate.Name == "T");
        _dark = DesignSystemResolver.Resolve(_application, theme, "dark")!;
        _light = DesignSystemResolver.Resolve(_application, theme, "light")!;
        _dusk = DesignSystemResolver.Resolve(_application, _application.Themes!.First(candidate => candidate.Name == "Dusk"), "dark")!;
        _office = DesignSystemResolver.Resolve(_application, _application.UiProfiles!.First(profile => profile.Name == "P"))!;
        _floor = DesignSystemResolver.Resolve(_application, _application.UiProfiles!.First(profile => profile.Name == "Floor"))!;
    }

    [Fact] void should_resolve_a_token_through_the_palette() => _light.Tokens["color.text"].ShouldEqual("#111827");
    [Fact] void should_resolve_the_mode_specific_value() => (_light.Tokens["color.surface"], _dark.Tokens["color.surface"]).ShouldEqual(("#FFFFFF", "#111827"));
    [Fact] void should_resolve_a_mode_independent_value_in_every_mode() => (_light.Tokens["radius.control"], _dark.Tokens["radius.control"]).ShouldEqual(("6px", "6px"));
    [Fact] void should_resolve_a_token_through_another_token() => _dusk.Tokens["color.primary"].ShouldEqual("#FFFFFF");
    [Fact] void should_resolve_the_component_parts() => _light.Components.Single().Variants.Single().Parts["background"].ShouldEqual("#1D4ED8");
    [Fact] void should_resolve_the_state_over_the_variant_parts() => _light.Components.Single().Variants.Single().States["disabled"]["background"].ShouldEqual("#FFFFFF");
    [Fact] void should_keep_the_variant_parts_a_state_does_not_override() => _light.Components.Single().Variants.Single().States["hover"]["text"].ShouldEqual("#FFFFFF");
    [Fact] void should_use_the_default_theme_for_a_profile_without_a_theme_line() => _office.Theme.ShouldEqual("T");
    [Fact] void should_use_the_mode_the_profile_asks_for() => (_office.Mode, _floor.Mode).ShouldEqual(("dark", "light"));
    [Fact] void should_use_the_theme_a_profile_selects() => _floor.Theme.ShouldEqual("Dusk");
    [Fact] void should_report_no_problems() => _light.Problems.ShouldBeEmpty();
    [Fact] void should_not_resolve_a_theme_that_does_not_implement_the_design_system() => DesignSystemResolver.Resolve(_application, new ThemeSyntax("Other", [], SourceLocation.Start), "light").ShouldBeNull();
    [Fact] void should_not_resolve_a_mode_the_design_system_does_not_declare() => DesignSystemResolver.Resolve(_application, _application.Themes!.First(), "dusk").ShouldBeNull();
}
