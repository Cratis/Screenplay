// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_traversing_original_children_once : Specification
{
    ApplicationSyntax _syntax = null!;
    ImmutableArray<WorkspaceSyntaxEntry> _entries;
    int _moduleEnumerations;
    int _featureEnumerations;
    int _sliceEnumerations;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            module Billing
              feature Accounts
                slice StateChange Register
                  command Register
                    produces Opened
                  event Opened
            """).Value!;
        _syntax = parsed with { Modules = Modules(parsed.Modules) };
    }

    void Because() => _entries = WorkspaceSyntaxIndex.ForSyntax(_syntax, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));

    [Fact] void should_index_a_nonempty_tree() => _entries.Length.ShouldBeGreaterThan(6);
    [Fact] void should_enumerate_the_root_children_once() => _moduleEnumerations.ShouldEqual(1);
    [Fact] void should_not_serialize_the_module_subtree() => _featureEnumerations.ShouldEqual(1);
    [Fact] void should_not_serialize_any_ancestor_of_the_slice() => _sliceEnumerations.ShouldEqual(1);
    [Fact] void should_not_invent_catalog_assignments() => _entries.All(entry => entry.SemanticId is null && entry.EventContractId is null).ShouldBeTrue();

    IEnumerable<ModuleSyntax> Modules(IEnumerable<ModuleSyntax> modules)
    {
        _moduleEnumerations++;
        foreach (var module in modules)
        {
            yield return module with { Features = Features(module.Features) };
        }
    }

    IEnumerable<FeatureSyntax> Features(IEnumerable<FeatureSyntax> features)
    {
        _featureEnumerations++;
        foreach (var feature in features)
        {
            yield return feature with { Slices = Slices(feature.Slices) };
        }
    }

    IEnumerable<SliceSyntax> Slices(IEnumerable<SliceSyntax> slices)
    {
        _sliceEnumerations++;
        foreach (var slice in slices)
        {
            yield return slice;
        }
    }
}
