// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_RenameCommand;

public class when_renaming_an_event : Tool.given.a_renderable_model
{
    int _exitCode;
    SemanticId? _before;
    ModelSourceLoad _after;

    void Establish() => _before = Identity(ModelSource.Load(Root).Workspace, "ProjectRegistered");

    void Because()
    {
        _exitCode = ToolCommands.Run(["rename", "Projects.Registration.RegisterProject.ProjectRegistered", "ProjectEnrolled", Root], Output, Error);
        _after = ModelSource.Load(Root);
    }

    static SemanticId? Identity(ScreenplayWorkspace workspace, string name) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Single(entry => entry.Address is { Kind: SemanticKind.EventContract } address && address.Name == name).SemanticId;

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_rename_the_event_in_source() => File.ReadAllText(Path.Combine(Root, "application.play")).ShouldContain("event ProjectEnrolled");
    [Fact] void should_write_the_identity_file() => File.Exists(Path.Combine(Root, ".screenplay", "identities.json")).ShouldBeTrue();
    [Fact] void should_open_with_persisted_identities() => _after.HasPersistedIdentities.ShouldBeTrue();
    [Fact] void should_keep_the_semantic_identity() => Identity(_after.Workspace, "ProjectEnrolled").ShouldEqual(_before);
    [Fact] void should_report_the_kept_identity() => Output.ToString().ShouldContain(_before.ToString()!);
}
