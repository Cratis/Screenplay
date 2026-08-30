// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSourceTokenizer;

public class when_tokenizing_empty_source : Specification
{
    WorkspaceTokenDocument _result;

    void Because() => _result = WorkspaceSourceTokenizer.Tokenize(WorkspaceDocument.Create(
        "empty",
        PortablePlayPath.Parse("Empty.play"),
        []));

    [Fact] void should_keep_the_document() => _result.Document.StableKey.ShouldEqual("empty");
    [Fact] void should_produce_no_tokens() => _result.Tokens.ShouldBeEmpty();
}
