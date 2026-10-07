// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SourceCommentCapture;

public class when_capturing_physical_header_comments : Specification
{
    readonly List<string> _ownershipFailures = [];
    readonly List<string> _printingFailures = [];
    ApplicationSyntax _root = null!;

    void Because()
    {
        foreach (var leading in new[] { string.Empty, "// Header lead\n" })
        {
            foreach (var (source, placement) in new (string, PlayPlacement)[]
            {
                ("module Example // Header", PlayPlacement.Document),
                ("module Example // Header", new(["Example"])),
                ("feature View // Header", new(["Example"])),
                ("slice StateView List // Header", new(["Example", "View"]))
            })
            {
                var parsed = ScreenplayCompiler.ParsePlaced(leading + source, "placed.play", placement, ScreenplayLanguageRegistry.Default);
                var application = parsed.Value!;
                SyntaxNode owner = application.Modules.Single();
                if (source.StartsWith("feature", StringComparison.Ordinal)) owner = application.Modules.Single().Features.Single();
                if (source.StartsWith("slice", StringComparison.Ordinal)) owner = application.Modules.Single().Features.Single().Slices.Single();
                if (application.SourceComments.Length != 0 || owner.SourceComments.Count(comment => comment.Text == "// Header") != 1 ||
                    (leading.Length > 0 && owner.SourceComments.Count(comment => comment.Text == "// Header lead") != 1))
                {
                    _ownershipFailures.Add(leading + source);
                }

                // Restated placement headers are omitted in fragments and restored by folder assembly.
                var assembled = PlayFolderMerge.Merge([parsed]).Value!;
                var printed = new ScreenplayPrinter().Print(assembled);
                var normalized = string.Join('\n', printed.Split('\n').Select(line => line.TrimStart()));
                if (!normalized.Contains(source, StringComparison.Ordinal) ||
                    (leading.Length > 0 && !normalized.Contains(leading + source, StringComparison.Ordinal)))
                {
                    _printingFailures.Add($"{leading}{source}:\n{printed}");
                }
            }
        }

        _root = ScreenplayCompiler.ParsePlaced("// Root comment", "root.play", PlayPlacement.Document, ScreenplayLanguageRegistry.Default).Value!;
    }

    [Fact] void should_attach_each_comment_to_the_physical_declaration_instead_of_a_wrapper() => Assert.True(_ownershipFailures.Count == 0, string.Join('\n', _ownershipFailures));
    [Fact] void should_print_leading_and_trailing_comments_beside_the_same_header() => Assert.True(_printingFailures.Count == 0, string.Join('\n', _printingFailures));
    [Fact] void should_keep_unanchored_root_comments_on_the_application() => _root.SourceComments.Single().Text.ShouldEqual("// Root comment");
}
