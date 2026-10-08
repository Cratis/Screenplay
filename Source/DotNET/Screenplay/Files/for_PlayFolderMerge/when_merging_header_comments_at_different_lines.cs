// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_header_comments_at_different_lines : Specification
{
    ModuleSyntax _firstModule;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var first = compiler.Parse("""
            policy SignedIn
              require authenticated
            // First module explanation.
            module Example
              description "Example" // Module description.
              // First feature explanation.
              feature View
                description "View" // Feature description.
                slice StateView First
                // Feature ending.
              // Module ending.
            """,
            "First.play");
        _firstModule = first.Value!.Modules.Single();
        var result = PlayFolderMerge.Merge([
            first,
            compiler.Parse("""
                // Second module explanation.
                module Example
                  // Second feature explanation.
                  feature View
                    slice StateView Second
                """,
                "Second.play")
        ]);
        _module = result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(result.Value);
    }

    [Fact] void should_print_module_headers_in_file_order_despite_their_source_lines() => _printed.Contains("// First module explanation.\n// Second module explanation.\nmodule Example", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_print_feature_headers_in_file_order_despite_their_source_lines() => _printed.Contains("  // First feature explanation.\n  // Second feature explanation.\n  feature View", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_module_directive_comment_anchors() => _module.SourceComments.Single(comment => comment.Text == "// Module description.").ShouldEqual(_firstModule.SourceComments.Single(comment => comment.Text == "// Module description."));
    [Fact] void should_preserve_feature_directive_comment_anchors() => _module.Features.Single().SourceComments.Single(comment => comment.Text == "// Feature description.").ShouldEqual(_firstModule.Features.Single().SourceComments.Single(comment => comment.Text == "// Feature description."));
    [Fact] void should_preserve_module_end_comment_anchors() => _module.SourceComments.Single(comment => comment.Text == "// Module ending.").ShouldEqual(_firstModule.SourceComments.Single(comment => comment.Text == "// Module ending."));
    [Fact] void should_preserve_feature_end_comment_anchors() => _module.Features.Single().SourceComments.Single(comment => comment.Text == "// Feature ending.").ShouldEqual(_firstModule.Features.Single().SourceComments.Single(comment => comment.Text == "// Feature ending."));
    [Fact] void should_keep_the_module_directive_comment_inline() => _printed.Contains("description \"Example\" // Module description.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_feature_directive_comment_inline() => _printed.Contains("description \"View\" // Feature description.", StringComparison.Ordinal).ShouldBeTrue();
}
