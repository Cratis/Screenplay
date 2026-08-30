// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSourceTokenizer;

public class when_tokenizing_mixed_source_trivia : Specification
{
    const string Text = "  domain Projects  // domain comment\r\n\t// comment only\nconcept Name : String\r\n\r\nlast   ";
    WorkspaceTokenDocument _result;

    void Because() => _result = WorkspaceSourceTokenizer.Tokenize(WorkspaceDocument.Create(
        "mixed",
        PortablePlayPath.Parse("Mixed.play"),
        [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(Text)]));

    [Fact] void should_reconstruct_every_decoded_source_character() => string.Concat(_result.Tokens.Select(token => token.Text)).ShouldEqual(Text);
    [Fact] void should_classify_both_indentation_tokens() => Tokens(WorkspaceSourceTokenKind.Indentation).Select(token => token.Text).ShouldEqual("  ", "\t");
    [Fact] void should_classify_trailing_whitespace_outside_comments() => Tokens(WorkspaceSourceTokenKind.Whitespace).Select(token => token.Text).ShouldEqual("  ", "   ");
    [Fact] void should_classify_comments_with_their_complete_tail() => Tokens(WorkspaceSourceTokenKind.Comment).Select(token => token.Text).ShouldEqual("// domain comment", "// comment only");
    [Fact] void should_preserve_mixed_line_endings() => Tokens(WorkspaceSourceTokenKind.LineEnding).Select(token => token.Text).ShouldEqual("\r\n", "\n", "\r\n", "\r\n");
    [Fact] void should_offset_the_first_token_after_the_bom() => _result.Tokens[0].Span.ByteOffset.ShouldEqual(Encoding.UTF8.Preamble.Length);
    [Fact] void should_end_at_the_exact_original_byte_length() => (LastToken().Span.ByteOffset + LastToken().Span.ByteLength).ShouldEqual(_result.Document.Bytes.Length);

    WorkspaceSourceToken LastToken() => _result.Tokens[^1];

    IEnumerable<WorkspaceSourceToken> Tokens(WorkspaceSourceTokenKind kind) => _result.Tokens.Where(token => token.Kind == kind);
}
