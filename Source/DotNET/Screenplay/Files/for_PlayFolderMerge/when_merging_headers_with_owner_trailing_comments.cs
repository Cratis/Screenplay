// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_headers_with_owner_trailing_comments : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("""
                module Example // Module note.
                  feature View // Feature note.
                    slice StateView First
                """,
                "First.play"),
            compiler.Parse("""
                // Second module explanation.
                module Example // Second module note.
                  // Second feature explanation.
                  feature View // Second feature note.
                    slice StateView Second
                """,
                "Second.play")
        ]);
        _module = _result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value);
    }

    [Fact] void should_keep_the_owner_module_comment_trailing() => _module.SourceComments.Single(comment => comment.Text == "// Module note.").Placement.ShouldEqual(SourceCommentPlacement.Trailing);
    [Fact] void should_keep_the_owner_feature_comment_trailing() => _module.Features.Single().SourceComments.Single(comment => comment.Text == "// Feature note.").Placement.ShouldEqual(SourceCommentPlacement.Trailing);
    [Fact] void should_print_the_owner_module_comment_inline_and_other_comments_above_it() => _printed.Contains("// Second module explanation.\n// Second module note.\nmodule Example // Module note.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_print_the_owner_feature_comment_inline_and_other_comments_above_it() => _printed.Contains("  // Second feature explanation.\n  // Second feature note.\n  feature View // Feature note.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
