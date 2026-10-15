// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// The conformance vector for template applicability. Each row is a case of Scene's shared
/// <c>template-applicability-fixtures.json</c> (Cratis/Scene, <c>ApplicableTemplates</c>) restated as the
/// Screenplay source an author would write, so the same scope, slice-kind and nesting decisions hold in both:
/// explicit <c>scopes</c> override the defaults, a slice kind narrows a slice scope, and a slice that presents
/// nothing takes no template.
/// </summary>
public class when_checking_template_applicability_against_the_scene_vectors : given.a_compiler
{
    // template scopes line, where it is used, the slice type of the target, whether Scene's ApplicableTemplates offers it there.
    [Theory]
    [InlineData("scopes feature", "feature", "StateView", true)]                                                  // pinned dashboard at Feature
    [InlineData("scopes feature", "module", "StateView", false)]                                                  // explicit scopes override the default
    [InlineData("scopes subfeature", "feature", "StateView", false)]                                              // subfeature is its own scope
    [InlineData("scopes slice stateview", "slice", "StateView", true)]                                            // state view slice lists data display fragments
    [InlineData("scopes slice stateview", "slice", "StateChange", false)]                                         // ... and not on a state change slice
    [InlineData("scopes slice statechange", "slice", "StateChange", true)]                                        // state change slice lists dialog and form templates
    [InlineData("scopes slice statechange", "slice", "StateView", false)]
    [InlineData("scopes slice statechange|stateview", "slice", "StateView", true)]
    [InlineData("scopes slice", "slice", "StateChange", true)]                                                    // no kinds declared: any slice that presents something
    [InlineData("scopes slice", "slice", "StateView", true)]
    [InlineData("scopes slice", "slice", "Automation", false)]                                                    // an automation slice has no presentation
    [InlineData("scopes slice statechange|stateview", "slice", "Automation", false)]
    [InlineData("scopes none", "module", "StateView", false)]                                                     // an explicitly empty list applies nowhere
    [InlineData("scopes none", "slice", "StateView", false)]
    [InlineData("scopes module, feature, subfeature", "subfeature", "StateView", true)]
    void should_agree_with_the_applicable_templates_of_scene(string scopes, string where, string sliceType, bool offered)
    {
        var source = $"module Sales\n  screen template Page\n    category Page\n    {scopes}\n    body\n\n{Use(where, sliceType)}";
        var mismatches = _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.TemplateScopeMismatch).ToList();
        (mismatches.Count == 0).ShouldEqual(offered);
    }

    [Fact]
    void should_not_restrict_a_template_that_declares_no_scopes()
    {
        // The structural role decides; Scene's per-level defaults are a picker's concern, not a validity rule for an authored reference.
        var source = "module Sales\n  screen template Page\n    category Page\n    body\n\n" + Use("slice", "StateView");
        _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.TemplateScopeMismatch).ShouldBeEmpty();
    }

    static string Use(string where, string sliceType) => where switch
    {
        "module" => "  template Page\n",
        "feature" => "  feature F\n    template Page\n",
        "subfeature" => "  feature F\n    feature G\n      template Page\n",
        _ => $"  feature F\n    slice {sliceType} S\n      template Page\n"
    };
}
