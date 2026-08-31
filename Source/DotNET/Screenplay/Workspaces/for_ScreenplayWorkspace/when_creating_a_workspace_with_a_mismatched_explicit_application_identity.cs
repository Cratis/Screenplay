// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_creating_a_workspace_with_a_mismatched_explicit_application_identity : given.a_valid_workspace
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ScreenplayWorkspace.Create(
        ApplicationIdentity.Create("studio-application-42"),
        "Projects",
        [Registration, Concepts],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("another-application"))));

    [Fact] void should_reject_the_workspace() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
}
