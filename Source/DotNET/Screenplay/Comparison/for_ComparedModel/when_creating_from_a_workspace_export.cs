// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ComparedModel;

public class when_creating_from_a_workspace_export : Specification
{
    ComparedModel _original = null!;
    ComparedModel _result = null!;

    void Establish() => _original = ComparedModel.FromSources("Projects", new Dictionary<string, string> { ["application.play"] = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n" });
    void Because() => _result = ComparedModel.FromWorkspaceExport(ScreenplayWorkspaceSerializer.Serialize(_original.Workspace));

    [Fact] void should_use_authoritative_identities() => _result.HasPersistedIdentities.ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _result.Workspace.Revision.ShouldEqual(_original.Workspace.Revision);
}
