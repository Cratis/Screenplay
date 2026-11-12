// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_secret : given.a_refactoring_workspace
{
    const string Source = """
        secret Key
        secret Other
        system Mailer
          ability Send idempotent
          setting apiKey from secret Key
          setting token from secret Other
        system Pay
          ability Charge idempotent
          setting apiKey from secret Key
        """;

    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<SecretSyntax>("Key", "MailKey"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration_and_every_directive_that_uses_it() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("secret Key", "secret MailKey", StringComparison.Ordinal).Replace("from secret Key", "from secret MailKey", StringComparison.Ordinal));
    [Fact] void should_keep_every_sink_reference_resolved() => new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(_result.Workspace)).Bindings.Where(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Secret).All(binding => binding.Target is not null).ShouldBeTrue();
    [Fact]
    void should_refuse_a_name_that_differs_only_in_case_from_another_sink()
    {
        var refused = Workspace.ProposeRename(Rename<SecretSyntax>("Key", "other"));
        refused.Accepted.ShouldBeFalse();
        refused.Conflicts.Single().Message.ShouldContain("already declares secret 'other'");
    }
}
