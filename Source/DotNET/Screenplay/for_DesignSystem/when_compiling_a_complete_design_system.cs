// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_DesignSystem;

public class when_compiling_a_complete_design_system : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    DesignSystemSyntax _designSystem;
    ThemeSyntax _theme;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;
    ApplicationSyntax _deserialized;

    void Because()
    {
        _result = _compiler.Compile(given.a_design_system_document.Source);
        _designSystem = _result.Value!.DesignSystems.Single();
        _theme = _result.Value!.Themes!.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _reprinted = _compiler.Compile(_printed);
        _deserialized = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_result.Value!));
    }

    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_modes_and_the_default_theme() => (string.Join(",", _designSystem.Modes), _designSystem.DefaultTheme).ShouldEqual(("light,dark", "T"));
    [Fact] void should_carry_the_token_categories_with_their_descriptions() => _designSystem.Tokens.Select(category => category.Category).ShouldEqual("color", "space", "radius");
    [Fact] void should_carry_a_token_description() => _designSystem.Tokens.First().Tokens.First().Description.ShouldEqual("page background");
    [Fact] void should_carry_the_component_parts() => _designSystem.Components.Single().Parts.ShouldEqual("text", "background", "radius");
    [Fact] void should_carry_the_variant_states() => _designSystem.Components.Single().Variants.Single().States.Select(state => state.Name).ShouldEqual("hover", "disabled");
    [Fact] void should_carry_the_words() => (_designSystem.Words!.Voice.Single().Value, _designSystem.Words.Naming.Single().Name).ShouldEqual(("Plain", "workspace"));
    [Fact] void should_carry_the_contrast_rule() => (_designSystem.Rules.Single().Foreground, _designSystem.Rules.Single().Background, _designSystem.Rules.Single().Minimum).ShouldEqual(("text", "background", "4.5"));
    [Fact] void should_carry_what_the_theme_implements() => _theme.Implements.ShouldEqual("D");
    [Fact] void should_carry_the_palette_values_and_mode_values() => (_theme.Palette.Count(), _theme.Values.Count(), string.Join(",", _theme.ModeValues.Select(block => block.Mode))).ShouldEqual((3, 2, "light,dark"));
    [Fact] void should_keep_the_package_compatibility() => _theme.CompatibleWith.ShouldEqual("core");
    [Fact] void should_print_canonically_and_compile_the_print_without_diagnostics() => _reprinted.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_the_second_pass() => new ScreenplayPrinter().Print(_reprinted.Value!).ShouldEqual(_printed);
    [Fact] void should_round_trip_through_syntax_json_losslessly() => SyntaxJson.StructurallyEqual(_result.Value!, _deserialized).ShouldBeTrue();
    [Fact] void should_not_serialize_what_an_older_theme_never_had() => SyntaxJson.Serialize(new ThemeSyntax("Old", ["core"], SourceLocation.Start)).GetRawText().ShouldEqual("{\"kind\":\"ThemeSyntax\",\"compatibleWith\":[\"core\"],\"name\":\"Old\"}");
}
