// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The design system vectors: a positive document with a design system and two themes implementing it, a deprecation vector for the
/// profile appearance members the themes replace, and what a host such as Stage or Scene reads through the resolver.
/// </summary>
public class when_compiling_the_design_system_vectors : Specification
{
    CanonicalScreenCorpusVector _corpus;
    CanonicalTypedScreenSourceCase _case;
    CanonicalTypedScreenSourceCase _deprecated;
    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _deprecatedResult;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;
    ApplicationSyntax _deserialized;
    ResolvedDesignTheme _office;
    ResolvedDesignTheme _floor;

    void Because()
    {
        _corpus = ScreenCompositionCorpus.V1;
        _case = _corpus.TypedSourceCases.Single(sourceCase => sourceCase.Name == "design-system-positive");
        _deprecated = _corpus.TypedSourceCases.Single(sourceCase => sourceCase.Name == "ui-profile-appearance-deprecated");
        _result = new ScreenplayCompiler().Compile(_case.Document.Text);
        _deprecatedResult = new ScreenplayCompiler().Compile(_deprecated.Document.Text);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _reprinted = new ScreenplayCompiler().Compile(_printed);
        _deserialized = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_result.Value!));
        _office = DesignSystemResolver.Resolve(_result.Value!, _result.Value!.UiProfiles!.Single(profile => profile.Name == "Office"))!;
        _floor = DesignSystemResolver.Resolve(_result.Value!, _result.Value!.UiProfiles!.Single(profile => profile.Name == "Floor"))!;
    }

    [Fact] void should_compile_without_any_diagnostic() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_be_pending() => _case.PendingReason.ShouldBeNull();
    [Fact] void should_carry_the_design_system_modes() => _result.Value!.DesignSystems.Single().Modes.ShouldEqual("light", "dark");
    [Fact] void should_carry_two_themes_implementing_it() => _result.Value!.Themes!.Select(theme => theme.Implements).ShouldEqual("Contoso", "Contoso");
    [Fact] void should_print_canonically_and_compile_the_print_without_diagnostics() => _reprinted.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_the_second_pass() => new ScreenplayPrinter().Print(_reprinted.Value!).ShouldEqual(_printed);
    [Fact] void should_round_trip_through_syntax_json_losslessly() => SyntaxJson.StructurallyEqual(_result.Value!, _deserialized).ShouldBeTrue();
    [Fact] void should_resolve_the_default_theme_for_a_profile_without_a_theme_line() => (_office.Theme, _office.Mode).ShouldEqual(("Aurora", "dark"));
    [Fact] void should_resolve_the_selected_theme_for_a_profile_with_one() => (_floor.Theme, _floor.Mode).ShouldEqual(("AuroraSharp", "light"));
    [Fact] void should_resolve_a_token_through_the_palette_in_the_mode() => (_office.Tokens["color.primary"], _floor.Tokens["color.primary"]).ShouldEqual(("#4338CA", "#0F766E"));
    [Fact] void should_resolve_mode_independent_values() => (_office.Tokens["radius.control"], _floor.Tokens["radius.control"]).ShouldEqual(("8px", "2px"));
    [Fact] void should_resolve_the_component_parts() => _office.Components.Single(component => component.Name == "Button").Variants.Single(variant => variant.Name == "primary").Parts["background"].ShouldEqual("#4338CA");
    [Fact] void should_resolve_the_state_over_the_variant() => _office.Components.Single(component => component.Name == "Button").Variants.Single(variant => variant.Name == "primary").States["hover"]["background"].ShouldEqual("#4F46E5");
    [Fact] void should_still_succeed_with_the_deprecated_appearance() => _deprecatedResult.Success.ShouldBeTrue();
    [Fact] void should_warn_only_about_the_deprecated_appearance_members() => _deprecatedResult.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Severity)).Distinct().ShouldEqual((DiagnosticCodes.DeprecatedAppearanceMember, DiagnosticSeverity.Warning));
    [Fact] void should_warn_once_for_each_deprecated_member() => _deprecatedResult.Diagnostics.Count().ShouldEqual(3);
    [Fact] void should_keep_reading_the_deprecated_values() => (_deprecatedResult.Value!.UiProfiles!.Single().Appearance!.Accent, _deprecatedResult.Value!.UiProfiles!.Single().Appearance!.Corners, _deprecatedResult.Value!.UiProfiles!.Single().Appearance!.Font).ShouldEqual(("#4F46E5", "round", "inter"));
    [Fact] void should_resolve_nothing_without_a_design_system() => DesignSystemResolver.Resolve(_deprecatedResult.Value!, _deprecatedResult.Value!.UiProfiles!.Single()).ShouldBeNull();

    [Fact]
    void should_carry_every_expected_syntax_kind()
    {
        var kinds = new KindCollector();
        kinds.VisitApplication(_result.Value!);
        foreach (var kind in _case.ExpectedSyntaxKinds)
        {
            kinds.Kinds.ShouldContain(kind);
        }
    }

    sealed class KindCollector : ScreenplaySyntaxWalker
    {
        public HashSet<string> Kinds { get; } = [];

        public override void VisitNode(SyntaxNode node)
        {
            Kinds.Add(node.GetType().Name);
            base.VisitNode(node);
        }
    }
}
