// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_matching_timeline_findings : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n    slice StateChange W\n      event E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_keep_the_existing_finding() => TimelineOrder.Analyze(new ScreenplayCompiler().Parse(_source).Value!).Count.ShouldEqual(1);
}
