// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_implying_feature_levels : given.a_model
{
    IReadOnlyList<ImpliedDependency> _edges;

    void Establish() => _graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n        from E\n      command C\n        reads R\n    feature Sub\n      slice StateChange W\n        event E\n        projection R\n          from E\nmodule N\n  feature G\n    slice StateView Other\n      projection P\n        from E\n");
    void Because() => _edges = _graph.Implied("feature", "feature");

    [Fact] void should_exclude_an_ancestor_feature() => _edges.Any(edge => edge.Source.Address == "M.F" && edge.Target.Address == "M.F.Sub").ShouldBeFalse();
}
