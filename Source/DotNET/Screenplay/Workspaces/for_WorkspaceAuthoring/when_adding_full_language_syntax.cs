// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_adding_full_language_syntax : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(
        new AddWorkspaceNode(RegistrationRoot.Handle, RegistrationRoot.Node, "imports", new ImportSyntax("External.Unused", SourceLocation.Start))));

    [Fact] void should_accept_source_authoring() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_claim_executable_readiness() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_explain_the_backend_limit() => _result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_preserve_every_assigned_semantic_identity() => Workspace.IdentityCatalog.Semantics.All(_result.Workspace.IdentityCatalog.Semantics.Contains).ShouldBeTrue();
    [Fact] void should_preserve_every_event_contract_identity() => Workspace.IdentityCatalog.EventContracts.All(_result.Workspace.IdentityCatalog.EventContracts.Contains).ShouldBeTrue();
    [Fact] void should_preserve_untouched_source_bytes() => _result.Workspace.Documents.Single(document => document.Id == Concepts.Id).Bytes.AsSpan().SequenceEqual(Concepts.Bytes.AsSpan()).ShouldBeTrue();
    [Fact] void should_disclose_normalization_and_comment_loss() => _result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeTrue();
    [Fact] void should_round_trip_the_exact_workspace_transport() => ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(_result.Workspace)).Revision.ShouldEqual(_result.Workspace.Revision);
}
