// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SourceCommentCapture;

public class when_capturing_physical_end_comments : Specification
{
    readonly List<string> _ownershipFailures = [];
    ApplicationSyntax _root = null!;

    void Because()
    {
        foreach (var (source, placement) in new (string, PlayPlacement)[]
        {
            ("module Example", PlayPlacement.Document),
            ("feature View", new(["Example"])),
            ("slice StateView List", new(["Example", "View"]))
        })
        {
            var application = ScreenplayCompiler.ParsePlaced(source + "\n  // End comment", "placed.play", placement, ScreenplayLanguageRegistry.Default).Value!;
            SyntaxNode owner = application.Modules.Single();
            if (source.StartsWith("feature", StringComparison.Ordinal)) owner = application.Modules.Single().Features.Single();
            if (source.StartsWith("slice", StringComparison.Ordinal)) owner = application.Modules.Single().Features.Single().Slices.Single();
            if (application.SourceComments.Length != 0 || owner.SourceComments.Count(comment => comment.Text == "// End comment" && comment.Placement == SourceCommentPlacement.End) != 1)
            {
                _ownershipFailures.Add(source);
            }
        }

        _root = ScreenplayCompiler.ParsePlaced("  // Root end comment", "root.play", PlayPlacement.Document, ScreenplayLanguageRegistry.Default).Value!;
    }

    [Fact] void should_attach_end_comments_to_the_physical_declaration_instead_of_a_wrapper() => Assert.True(_ownershipFailures.Count == 0, string.Join('\n', _ownershipFailures));
    [Fact] void should_keep_end_comments_on_the_root_if_there_is_no_physical_declaration() => _root.SourceComments.Single().Text.ShouldEqual("// Root end comment");
}
