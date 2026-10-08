// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_placed_headers_with_owner_trailing_comments : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("slice StateView Second", "A-placed.play", new(["Example", "View"])),
            compiler.Parse("""
                module Example // Module note.
                  feature View // Feature note.
                    slice StateView First
                """,
                "Owner.play")
        ]);
        _module = _result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value);
    }

    [Fact] void should_keep_the_owner_module_comment_trailing_despite_the_earlier_wrapper() => _module.SourceComments.Single().Placement.ShouldEqual(SourceCommentPlacement.Trailing);
    [Fact] void should_keep_the_owner_feature_comment_trailing_despite_the_earlier_wrapper() => _module.Features.Single().SourceComments.Single().Placement.ShouldEqual(SourceCommentPlacement.Trailing);
    [Fact] void should_print_the_owner_module_comment_inline() => _printed.Contains("module Example // Module note.\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_print_the_owner_feature_comment_inline() => _printed.Contains("  feature View // Feature note.\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
