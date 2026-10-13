// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSnapshot;

public class when_assessing_generation : Specification
{
    McpSnapshot _snapshot;
    CompilationResult<SemanticCompilation> _compilation;
    int _parsedDocuments;

    void Establish()
    {
        _snapshot = new(new Dictionary<string, string> { ["application.play"] = "concept Code : String\n  generate nanoid\n" });
        _ = _snapshot.Compilation;
        _parsedDocuments = _snapshot.ParsedDocumentCount;
    }

    void Because() => _compilation = _snapshot.ExecutableCompilation;

    [Fact] void should_bind_the_already_parsed_source() => _compilation.Success.ShouldBeTrue();
    [Fact] void should_not_reparse_documents() => _snapshot.ParsedDocumentCount.ShouldEqual(_parsedDocuments);
    [Fact] void should_reuse_the_semantic_result() => ReferenceEquals(_snapshot.ExecutableCompilation, _compilation).ShouldBeTrue();
    [Fact] void should_reuse_a_workspace_semantic_result() => ReferenceEquals(new McpSnapshot(_snapshot.Authoring, _compilation).ExecutableCompilation, _compilation).ShouldBeTrue();
}
