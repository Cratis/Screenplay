// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSourceTokenizer;

public class when_a_bare_carriage_return_is_inside_a_line : Specification
{
    const string Text = "domain A\rdomain B\r\n";
    WorkspaceTokenDocument _result;

    void Because() => _result = WorkspaceSourceTokenizer.Tokenize(WorkspaceDocument.Create(
        "bare-carriage-return",
        PortablePlayPath.Parse("BareCarriageReturn.play"),
        Encoding.UTF8.GetBytes(Text)));

    [Fact] void should_preserve_the_bare_carriage_return_as_text() => _result.Tokens.Single(token => token.Kind == WorkspaceSourceTokenKind.Text).Text.ShouldEqual("domain A\rdomain B");
    [Fact] void should_keep_the_text_on_the_parsers_first_line() => _result.Tokens.Single(token => token.Kind == WorkspaceSourceTokenKind.Text).Span.Line.ShouldEqual(1);
    [Fact] void should_recognize_only_the_crlf_as_the_line_ending() => _result.Tokens.Single(token => token.Kind == WorkspaceSourceTokenKind.LineEnding).Text.ShouldEqual("\r\n");
    [Fact] void should_reconstruct_every_source_character() => string.Concat(_result.Tokens.Select(token => token.Text)).ShouldEqual(Text);
}
