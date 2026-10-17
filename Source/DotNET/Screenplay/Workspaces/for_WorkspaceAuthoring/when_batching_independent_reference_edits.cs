// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_batching_independent_reference_edits : given.a_reference_batch
{
    WorkspaceAuthoringResult[] _results = [];

    void Establish() => ShippingWorkspace();
    void Because() => _results = [Propose(Workspace, WorkspaceAuthoringReferencePolicy.Safe, ("Orders/Ship.play", ChangedShipping)), Propose(Workspace, WorkspaceAuthoringReferencePolicy.Draft, ("Orders/Ship.play", ChangedShipping))];

    [Fact] void should_accept_safe_and_draft_batches() => _results.ShouldEachConformTo(result => result.Accepted);
    [Fact] void should_preserve_the_supplied_source() => _results.ShouldEachConformTo(result => result.Workspace!.Documents.Single(document => document.Path.Value == "Orders/Ship.play").Text == ChangedShipping);
    [Fact] void should_not_report_reference_debt() => _results.ShouldEachConformTo(result => !result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.StartsWith("Reference debt:", StringComparison.Ordinal)));
}
