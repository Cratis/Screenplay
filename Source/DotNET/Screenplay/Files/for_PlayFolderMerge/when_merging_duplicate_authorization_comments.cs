// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_duplicate_authorization_comments : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("""
                policy SignedIn
                  require authenticated
                module Example
                  // First module explanation.
                  // Shared module explanation.
                  authorize SignedIn
                  feature View
                    // First feature explanation.
                    // Shared feature explanation.
                    authorize SignedIn
                    slice StateView First
                """,
                "First.play"),
            compiler.Parse("""
                module Example
                  // Second module explanation.
                  // Shared module explanation.
                  authorize SignedIn
                  feature View
                    // Second feature explanation.
                    // Shared feature explanation.
                    authorize SignedIn
                    slice StateView Second
                """,
                "Second.play")
        ]);
        _module = _result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value);
    }

    [Fact] void should_keep_every_module_comment_occurrence() => _module.Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(["// First module explanation.", "// Shared module explanation.", "// Second module explanation.", "// Shared module explanation."]);
    [Fact] void should_keep_every_feature_comment_occurrence() => _module.Features.Single().Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(["// First feature explanation.", "// Shared feature explanation.", "// Second feature explanation.", "// Shared feature explanation."]);
    [Fact] void should_print_each_different_explanation_once() => _printed.Split('\n').Count(line => line.Trim() == "// First module explanation." || line.Trim() == "// Second module explanation." || line.Trim() == "// First feature explanation." || line.Trim() == "// Second feature explanation.").ShouldEqual(4);
    [Fact] void should_print_each_shared_explanation_twice() => _printed.Split('\n').Count(line => line.Trim() == "// Shared module explanation." || line.Trim() == "// Shared feature explanation.").ShouldEqual(4);
    [Fact] void should_keep_one_module_requirement() => _module.Authorize!.References().Select(reference => reference.Name).ShouldEqual(["SignedIn"]);
    [Fact] void should_keep_one_feature_requirement() => _module.Features.Single().Authorize!.References().Select(reference => reference.Name).ShouldEqual(["SignedIn"]);
    [Fact] void should_warn_about_both_duplicate_gates() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.DuplicateAuthorizationAcrossFiles, DiagnosticCodes.DuplicateAuthorizationAcrossFiles]);
    [Fact] void should_keep_duplicate_diagnostics_as_warnings() => _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
