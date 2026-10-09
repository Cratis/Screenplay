// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_compliance_markers : given.a_compiler
{
    [Theory]
    [InlineData("pii", "pii")]
    [InlineData("personal", "pii")]
    [InlineData("secret", "sensitive")]
    void should_accept_canonical_markers_and_the_personal_alias_without_diagnostics(string marker, string wire)
    {
        var result = _compiler.Compile($"concept Value : String {marker}");
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Concepts.Single().AttributeNames.ShouldContainOnly(wire);
    }

    [Theory]
    [InlineData("@pii")]
    [InlineData("sensitive")]
    [InlineData("@sensitive")]
    [InlineData("@pii @sensitive")]
    void should_report_one_information_diagnostic_per_legacy_line(string markers)
    {
        var result = _compiler.Compile($"concept Value : String {markers}");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.LegacyComplianceMarker);
        result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Information);
    }

    [Theory]
    [InlineData("@encrypted")]
    [InlineData("pi")]
    [InlineData("@personal")]
    [InlineData("@secret")]
    void should_reject_unknown_markers(string marker) =>
        _compiler.Compile($"concept Value : String {marker}").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownComplianceMarker);

    [Theory]
    [InlineData("subject")]
    [InlineData("namespace")]
    [InlineData("global")]
    void should_keep_explicit_secret_scope(string scope)
    {
        var result = _compiler.Compile($"concept Value : String secret\n  secret scope {scope}");
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Concepts.Single().Attributes.Single().Scope.ShouldEqual(scope);
    }

    [Theory]
    [InlineData("racialOrEthnicOrigin")]
    [InlineData("politicalOpinions")]
    [InlineData("religiousOrPhilosophicalBeliefs")]
    [InlineData("tradeUnionMembership")]
    [InlineData("genetic")]
    [InlineData("biometric")]
    [InlineData("health")]
    [InlineData("sexLifeOrSexualOrientation")]
    void should_keep_each_special_category_alongside_criminal_data(string category)
    {
        var result = _compiler.Compile($"concept Value : String personal\n  personal special {category}\n  personal criminal");
        result.Diagnostics.ShouldBeEmpty();
        var attribute = result.Value!.Concepts.Single().Attributes.Single();
        attribute.SpecialCategory.ShouldEqual(category);
        attribute.Criminal.ShouldBeTrue();
    }

    [Theory]
    [InlineData("pii", "pii scope subject", DiagnosticCodes.InvalidSecretScope)]
    [InlineData("secret", "secret scope tenant", DiagnosticCodes.InvalidSecretScope)]
    [InlineData("secret", "secret scope subject\n  secret scope namespace", DiagnosticCodes.DuplicateSecretScope)]
    [InlineData("pii", "pii special unknown", DiagnosticCodes.InvalidPersonalDataQualifier)]
    [InlineData("pii", "pii special health\n  pii special genetic", DiagnosticCodes.DuplicateSpecialCategory)]
    [InlineData("", "pii special health", DiagnosticCodes.AttributeReasonWithoutAttribute)]
    [InlineData("", "pii criminal", DiagnosticCodes.AttributeReasonWithoutAttribute)]
    [InlineData("secret", "pii criminal", DiagnosticCodes.AttributeReasonWithoutAttribute)]
    void should_reject_invalid_settings(string markers, string body, string code) =>
        _compiler.Compile($"concept Value : String {markers}".TrimEnd() + $"\n  {body}").Diagnostics.Single().Code.ShouldEqual(code);

    [Fact]
    void should_warn_about_scope_that_does_not_render_on_combined_markers()
    {
        var result = _compiler.Compile("concept Value : String pii secret\n  secret scope global");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.SecretScopeIgnoredForPii);
        result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    [Fact]
    void should_print_canonical_forms_and_roundtrip_every_setting()
    {
        var parsed = _compiler.Parse("concept Value : String personal\n  personal reason \"Keep @pii and sensitive in this note\"\n  personal special health\n  personal criminal\nconcept Key : String @sensitive\n  sensitive reason \"An API key\"\n  secret scope namespace");
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        printed.ShouldContain("concept Value : String pii");
        printed.ShouldContain("pii special health");
        printed.ShouldContain("pii criminal");
        printed.ShouldContain("concept Key : String secret");
        printed.ShouldContain("secret reason \"An API key\"");
        printed.ShouldContain("secret scope namespace");
        printed.ShouldContain("Keep @pii and sensitive in this note");
        var reparsed = _compiler.Parse(printed);
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
    }

    [Fact]
    void should_leave_default_scope_unwritten()
    {
        var parsed = _compiler.Parse("concept Key : String secret");
        parsed.Value!.Concepts.Single().Attributes.Single().Scope.ShouldBeNull();
        new ScreenplayPrinter().Print(parsed.Value).ShouldNotContain("scope");
    }
}
