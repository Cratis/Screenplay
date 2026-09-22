// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpIndex;

public class when_reading_only_compilation_diagnostics : Specification
{
    McpSnapshot _snapshot = null!;
    bool _initiallyLazy;

    void Establish()
    {
        _snapshot = given.synthetic_model.Create(100);
        _initiallyLazy = !_snapshot.IsCompilationCreated && !_snapshot.IsIndexCreated && _snapshot.ParsedDocumentCount == 0;
    }

    void Because() => _ = _snapshot.Compilation.Diagnostics;

    [Fact] void should_defer_all_analysis_until_requested() => _initiallyLazy.ShouldBeTrue();
    [Fact] void should_compile_on_demand() => _snapshot.IsCompilationCreated.ShouldBeTrue();
    [Fact] void should_parse_the_document_population_once() => _snapshot.ParsedDocumentCount.ShouldEqual(100);
    [Fact] void should_not_construct_a_query_index() => _snapshot.IsIndexCreated.ShouldBeFalse();
}
