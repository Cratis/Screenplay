// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_hint_free_documents : Specification
{
    static IEnumerable<(string Name, string Output)> Cases => PrinterMatrix.Run();

    [Fact]
    void should_cover_every_expected_case() => Cases.Select(c => c.Name).Order(StringComparer.Ordinal)
        .SequenceEqual(PrinterMatrixExpected.Outputs.Keys.Order(StringComparer.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_print_exactly_what_the_merge_base_printed() => Cases
        .Where(c => PrinterMatrixExpected.Outputs[c.Name] != c.Output).Select(c => c.Name).ShouldBeEmpty();

    // PreserveTrivia is covered by the merge-base differential above.
    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_keep_first_comments_on_the_renamed_rule_and_second_on_the_unchanged_rule(WorkspaceAuthoringFormatting formatting)
    {
        var text = PrinterMatrix.Propose(PrinterMatrix.TwoRules, block: true, "rename-first", formatting);
        text.ShouldContain("label rule Renamed // first");
        text.ShouldContain("file Same.cs // file first");
        text.ShouldContain("label rule Check // second");
        text.ShouldContain("file Same.cs // file second");
    }

    // PreserveTrivia is covered by the merge-base differential above.
    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_keep_every_comment_of_identical_blocks_when_a_distinct_block_is_added(WorkspaceAuthoringFormatting formatting)
    {
        var text = PrinterMatrix.Propose(PrinterMatrix.TwoBlocks, block: false, "append-third", formatting);
        foreach (var comment in new[] { "// block one", "// one rule", "// one file", "// block two", "// two rule", "// two file" })
        {
            text.ShouldContain(comment);
        }
    }
}
