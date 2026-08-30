// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSourceTokenizer;

public class when_tokenizing_multibyte_source : Specification
{
    const string Text = "concept Café : String // ☕\n";
    WorkspaceTokenDocument _result;

    void Because() => _result = WorkspaceSourceTokenizer.Tokenize(WorkspaceDocument.Create(
        "multibyte",
        PortablePlayPath.Parse("Multibyte.play"),
        Encoding.UTF8.GetBytes(Text)));

    [Fact] void should_preserve_utf16_text_ranges() => _result.Tokens.Single(token => token.Kind == WorkspaceSourceTokenKind.Comment).Span.TextLength.ShouldEqual("// ☕".Length);
    [Fact] void should_preserve_utf8_byte_ranges() => _result.Tokens.Single(token => token.Kind == WorkspaceSourceTokenKind.Comment).Span.ByteLength.ShouldEqual(Encoding.UTF8.GetByteCount("// ☕"));
    [Fact] void should_cover_every_source_byte_exactly_once() => _result.Tokens.Sum(token => token.Span.ByteLength).ShouldEqual(Encoding.UTF8.GetByteCount(Text));
    [Fact] void should_cover_every_text_unit_exactly_once() => _result.Tokens.Sum(token => token.Span.TextLength).ShouldEqual(Text.Length);
}
