// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_duplicate_header_comments : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("""
                // First module explanation.
                // Shared module explanation.
                module Example // Shared module header.
                  // First feature explanation.
                  // Shared feature explanation.
                  feature View // Shared feature header.
                    slice StateView First
                """,
                "First.play"),
            compiler.Parse("""
                // Second module explanation.
                // Shared module explanation.
                module Example // Shared module header.
                  // Second feature explanation.
                  // Shared feature explanation.
                  feature View // Shared feature header.
                    slice StateView Second
                """,
                "Second.play")
        ]);
        _module = _result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value);
    }

    [Fact] void should_keep_every_module_comment_occurrence() => _module.SourceComments.Select(comment => comment.Text).ShouldEqual(["// First module explanation.", "// Shared module explanation.", "// Shared module header.", "// Second module explanation.", "// Shared module explanation.", "// Shared module header."]);
    [Fact] void should_keep_every_feature_comment_occurrence() => _module.Features.Single().SourceComments.Select(comment => comment.Text).ShouldEqual(["// First feature explanation.", "// Shared feature explanation.", "// Shared feature header.", "// Second feature explanation.", "// Shared feature explanation.", "// Shared feature header."]);
    [Fact] void should_print_each_shared_module_explanation_twice() => _printed.Split('\n').Count(line => line.Trim() == "// Shared module explanation.").ShouldEqual(2);
    [Fact] void should_print_each_shared_module_header_comment_twice() => _printed.Split('\n').Count(line => line.Contains("// Shared module header.", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_print_each_shared_feature_explanation_twice() => _printed.Split('\n').Count(line => line.Trim() == "// Shared feature explanation.").ShouldEqual(2);
    [Fact] void should_print_each_shared_feature_header_comment_twice() => _printed.Split('\n').Count(line => line.Contains("// Shared feature header.", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_print_module_leading_comments_in_file_then_source_order_and_keep_the_owner_comment_inline() => _printed.Contains("// First module explanation.\n// Shared module explanation.\n// Second module explanation.\n// Shared module explanation.\n// Shared module header.\nmodule Example // Shared module header.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_print_feature_leading_comments_in_file_then_source_order_and_keep_the_owner_comment_inline() => _printed.Contains("  // First feature explanation.\n  // Shared feature explanation.\n  // Second feature explanation.\n  // Shared feature explanation.\n  // Shared feature header.\n  feature View // Shared feature header.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
