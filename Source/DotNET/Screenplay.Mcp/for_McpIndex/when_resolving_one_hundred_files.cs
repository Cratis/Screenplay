// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_resolving_one_hundred_files : Specification
{
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;
    bool _sameResults;

    void Establish() => _snapshot = given.synthetic_model.Create(100);

    void Because()
    {
        _index = _snapshot.Index;
        var references = _index.References.ToArray();
        _sameResults = Enumerable.Range(0, 10).All(_ => references.All(reference => ReferenceEquals(
            _index.Resolve(reference),
            _index.Resolve(reference with { Kinds = [.. reference.Kinds], Scope = [.. reference.Scope] }))));
    }

    [Fact] void should_parse_each_file_once() => _snapshot.ParsedDocumentCount.ShouldEqual(100);
    [Fact] void should_retain_every_leaf() => _index.Declarations.Count().ShouldEqual(302);
    [Fact] void should_resolve_each_reference_once() => _index.ResolutionCount.ShouldEqual(100);
    [Fact] void should_inspect_only_the_nearest_candidates() => _index.CandidateInspectionCount.ShouldEqual(100);
    [Fact] void should_reuse_resolution_arrays_for_equivalent_new_references() => _sameResults.ShouldBeTrue();
    [Fact] void should_keep_every_module_fragment() => _index.Find("Billing", "Module").Single().Locations.Count().ShouldEqual(100);
    [Fact] void should_keep_every_feature_fragment() => _index.Find("Billing.Accounts", "Feature").Single().Locations.Count().ShouldEqual(100);
    [Fact] void should_find_the_incoming_reference() => _index.Incoming(_index.Find("Billing.Accounts.Register99.Opened", "Event").Single()).Single().Reference.Owner.Address.ShouldEqual("Billing.Accounts.Register99.Register");
}
