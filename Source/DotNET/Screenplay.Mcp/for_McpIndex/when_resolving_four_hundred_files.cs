// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_resolving_four_hundred_files : Specification
{
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;
    int _outgoingCount;

    void Establish() => _snapshot = given.synthetic_model.Create(400);

    void Because()
    {
        _index = _snapshot.Index;
        foreach (var reference in _index.References)
        {
            _index.Resolve(reference);
            _index.Resolve(reference);
            _outgoingCount += _index.Outgoing(reference.Owner).Count();
        }
    }

    [Fact] void should_parse_each_file_once() => _snapshot.ParsedDocumentCount.ShouldEqual(400);
    [Fact] void should_retain_every_leaf() => _index.Declarations.Count().ShouldEqual(1202);
    [Fact] void should_resolve_each_reference_once() => _index.ResolutionCount.ShouldEqual(400);
    [Fact] void should_scale_candidate_inspection_with_references_not_all_declarations() => _index.CandidateInspectionCount.ShouldEqual(400);
    [Fact] void should_retrieve_each_owners_outgoing_reference() => _outgoingCount.ShouldEqual(400);
    [Fact] void should_expose_merged_module_meaning() => ((ModuleSyntax)_index.Find("Billing", "Module").Single().Syntax).Features.Single().Slices.Count().ShouldEqual(400);
}
