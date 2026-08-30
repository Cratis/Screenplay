// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSourceTokenizer;

public class when_comment_markers_are_inside_expressions : Specification
{
    const string Text = "value = \"https://example.test//path\" // actual\ntemplate = `// ${value}` // second";
    WorkspaceTokenDocument _result;

    void Because() => _result = WorkspaceSourceTokenizer.Tokenize(WorkspaceDocument.Create(
        "expressions",
        PortablePlayPath.Parse("Expressions.play"),
        Encoding.UTF8.GetBytes(Text)));

    [Fact] void should_keep_string_comment_markers_in_text() => TextTokens()[0].Text.ShouldContain("https://example.test//path");
    [Fact] void should_keep_template_comment_markers_in_text() => TextTokens()[^1].Text.ShouldContain("`// ${value}`");
    [Fact] void should_find_only_actual_comments() => _result.Tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Comment).Select(token => token.Text).ShouldEqual("// actual", "// second");

    WorkspaceSourceToken[] TextTokens() => [.. _result.Tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Text)];
}
