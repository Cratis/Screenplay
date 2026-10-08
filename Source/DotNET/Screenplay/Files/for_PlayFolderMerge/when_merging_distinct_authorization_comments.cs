// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_distinct_authorization_comments : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _roundTripped;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("""
                policy SignedIn
                  require authenticated
                policy Allowed
                  require authenticated
                module Example
                  // First explanation.
                  // First continuation.
                  authorize SignedIn // First trailing explanation.
                """,
                "First.play"),
            compiler.Parse("""
                module Example
                  // Second explanation.
                  // Second continuation.
                  authorize Allowed // Second trailing explanation.
                """,
                "Second.play")
        ]);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _roundTripped = compiler.Parse(_printed);
    }

    [Fact] void should_print_each_file_explanation_together_in_source_order() => _printed.Contains("  // First explanation.\n  // First continuation.\n  // First trailing explanation.\n  // Second explanation.\n  // Second continuation.\n  // Second trailing explanation.\n  authorize SignedIn and Allowed", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_both_distinct_requirements() => _result.Value!.Modules.Single().Authorize!.References().Select(reference => reference.Name).ShouldEqual(["SignedIn", "Allowed"]);
    [Fact] void should_not_warn_about_distinct_gates() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_successfully() => _roundTripped.Success.ShouldBeTrue();
    [Fact] void should_keep_every_comment_occurrence_after_reparsing() => _roundTripped.Value!.Modules.Single().Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(["// First explanation.", "// First continuation.", "// First trailing explanation.", "// Second explanation.", "// Second continuation.", "// Second trailing explanation."]);
}
