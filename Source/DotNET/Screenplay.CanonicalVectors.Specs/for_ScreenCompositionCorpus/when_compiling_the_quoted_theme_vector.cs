// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The quoted theme vector: a ui profile names a theme a component package provides by its display name. The compiler cannot see package
/// themes, so a name that is not an identifier is not checked against the document. The exact names survive a print
/// and a syntax JSON round trip.
/// </summary>
public class when_compiling_the_quoted_theme_vector : Specification
{
    CanonicalTypedScreenSourceCase _case;
    CompilationResult<ApplicationSyntax> _result;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;
    ApplicationSyntax _deserialized;

    void Because()
    {
        _case = ScreenCompositionCorpus.V1.TypedSourceCases.Single(sourceCase => sourceCase.Name == "ui-profile-quoted-theme");
        _result = new ScreenplayCompiler().Compile(_case.Document.Text);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _reprinted = new ScreenplayCompiler().Compile(_printed);
        _deserialized = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_result.Value!));
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_compile_without_any_diagnostic() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_exact_names() => _result.Value!.UiProfiles!.Select(profile => profile.Theme).ShouldEqual("Scene Default Light", "Scene Default Dark");
    [Fact] void should_print_the_names_quoted() => _printed.ShouldContain("theme \"Scene Default Light\"");
    [Fact] void should_keep_the_names_when_reprinted() => _reprinted.Value!.UiProfiles!.Select(profile => profile.Theme).ShouldEqual("Scene Default Light", "Scene Default Dark");
    [Fact] void should_round_trip_through_syntax_json_losslessly() => SyntaxJson.StructurallyEqual(_result.Value!, _deserialized).ShouldBeTrue();
}
