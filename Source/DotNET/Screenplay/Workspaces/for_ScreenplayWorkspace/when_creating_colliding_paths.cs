// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_creating_colliding_paths : given.a_valid_workspace
{
    Exception _exception = null!;

    void Because()
    {
        var first = Document("first", "Module/Feature.play", "module First");
        var second = Document("second", "module/feature.play", "module Second");
        _exception = Catch.Exception(() => ScreenplayWorkspace.Create(
            "Projects",
            [first, second],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"))));
    }

    [Fact] void should_reject_the_workspace() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
}
