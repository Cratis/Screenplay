// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_collapsing_duplicate_compliance_markers : given.a_compiler
{
    [Theory]
    [InlineData("pii personal", "pii", "pii", "personal")]
    [InlineData("personal pii", "pii", "pii", "personal")]
    [InlineData("pii pii", "pii", "pii", "")]
    [InlineData("secret secret", "sensitive", "secret", "")]
    [InlineData("@pii @pii", "pii", "pii", "")]
    void should_keep_one_attribute_with_its_settings_and_warn_on_the_header(string markers, string wire, string canonical, string alias)
    {
        var result = _compiler.Parse($"concept Value : String {markers}\n  {canonical} reason \"Keep the note\"");
        var concept = result.Value!.Concepts.Single();
        concept.Attributes.Single().Name.ShouldEqual(wire);
        concept.Attributes.Single().Reason.ShouldEqual("Keep the note");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0653");
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual(concept.Location);
        diagnostic.Message.ShouldEqual($"Concept 'Value' repeats the '{canonical}' marker{(alias.Length > 0 ? $" - '{alias}' is the same marker as '{canonical}'" : string.Empty)} - remove the duplicate");
        new ScreenplayPrinter().Print(result.Value).ShouldContain($"concept Value : String {canonical}\n");
    }
}
