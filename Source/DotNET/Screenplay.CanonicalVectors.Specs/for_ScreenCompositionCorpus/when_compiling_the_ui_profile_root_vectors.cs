// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The ui profile root template vectors (<c>uiprofile-root-*</c> in the Scene shared fixtures): an own layout, an own application-scope
/// screen template and a blueprint template with a version pin as the shell, shell options, appearance and the new screens default,
/// the legacy <c>layout</c> line, and a negative vector for every authoring mistake. The profile must survive a print and a syntax JSON
/// round trip unchanged - nothing about the shell lives anywhere but in the document.
/// </summary>
public class when_compiling_the_ui_profile_root_vectors : Specification
{
    CanonicalScreenCorpusVector _corpus;
    CanonicalTypedScreenSourceCase _case;
    CanonicalTypedScreenSourceCase _legacy;
    CompilationResult<ApplicationSyntax> _root;
    CompilationResult<ApplicationSyntax> _legacyResult;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;
    ApplicationSyntax _deserialized;

    void Because()
    {
        _corpus = ScreenCompositionCorpus.V1;
        _case = _corpus.TypedSourceCases.Single(sourceCase => sourceCase.Name == "ui-profile-root-positive");
        _legacy = _corpus.TypedSourceCases.Single(sourceCase => sourceCase.Name == "ui-profile-root-legacy-layout");
        _root = new ScreenplayCompiler().Compile(_case.Document.Text);
        _legacyResult = new ScreenplayCompiler().Compile(_legacy.Document.Text);
        _printed = new ScreenplayPrinter().Print(_root.Value!);
        _reprinted = new ScreenplayCompiler().Compile(_printed);
        _deserialized = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_root.Value!));
    }

    [Fact] void should_compile_the_root_vector_without_any_diagnostic() => _root.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_be_pending() => _case.PendingReason.ShouldBeNull();
    [Fact] void should_carry_an_own_layout_root_with_an_option() => (Profile("Desktop").Root!.Name, Profile("Desktop").Root!.Blueprint, Profile("Desktop").Root!.Options.Single().Name, Profile("Desktop").Root!.Options.Single().Value).ShouldEqual(("AppShell", null, "showFooter", "true"));
    [Fact] void should_carry_an_own_application_scope_screen_template_root() => Profile("Docked").Root!.Name.ShouldEqual("Docked");
    [Fact] void should_carry_a_blueprint_root_with_its_pin() => (Profile("FromBlueprint").Root!.Blueprint, Profile("FromBlueprint").Root!.Name, Profile("FromBlueprint").Root!.Version).ShouldEqual(("Cratis.Blueprint.Default", "Frame", "^4.5"));
    [Fact] void should_carry_the_appearance() => (Profile("Desktop").Appearance!.ColorScheme, Profile("Desktop").Appearance!.Accent).ShouldEqual(("dark", "#4F46E5"));
    [Fact] void should_carry_the_new_screens_default() => (Profile("Desktop").NewScreenTemplate!.Name, Profile("FromBlueprint").NewScreenTemplate!.Blueprint).ShouldEqual(("Sheet", "Cratis.Blueprint.Default"));
    [Fact] void should_print_canonically_and_compile_the_print_without_diagnostics() => _reprinted.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_the_second_pass() => new ScreenplayPrinter().Print(_reprinted.Value!).ShouldEqual(_printed);
    [Fact] void should_round_trip_through_syntax_json_losslessly() => SyntaxJson.StructurallyEqual(_root.Value!, _deserialized).ShouldBeTrue();
    [Fact] void should_warn_only_about_the_legacy_layout_lines() => _legacyResult.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Severity)).Distinct().ShouldEqual((DiagnosticCodes.LegacyProfileLayout, DiagnosticSeverity.Warning));
    [Fact] void should_still_succeed_with_the_legacy_layout() => _legacyResult.Success.ShouldBeTrue();
    [Fact] void should_keep_reading_the_legacy_layout() => _legacyResult.Value!.UiProfiles!.First().Layout.ShouldEqual("AppShell");

    [Fact]
    void should_carry_every_expected_syntax_kind()
    {
        var kinds = new KindCollector();
        kinds.VisitApplication(_root.Value!);
        foreach (var kind in _case.ExpectedSyntaxKinds)
        {
            kinds.Kinds.ShouldContain(kind);
        }
    }

    UiProfileSyntax Profile(string name) => _root.Value!.UiProfiles!.Single(profile => profile.Name == name);

    sealed class KindCollector : ScreenplaySyntaxWalker
    {
        public HashSet<string> Kinds { get; } = [];

        public override void VisitNode(SyntaxNode node) => Kinds.Add(node.GetType().Name);
    }
}
