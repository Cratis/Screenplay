// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_produced_event_mapping : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _result = null!;
    SemanticSpecificationRun _before = null!;
    SemanticSpecificationRun _after = null!;

    void Establish() => _before = Run(_workspace);

    void Because()
    {
        _result = _workspace.Propose(Request(_operation));
        _after = Run(_result.Workspace!);
    }

    [Fact] void should_admit_the_patch() => _result.Success.ShouldBeTrue();
    [Fact] void should_expose_the_previous_behavior_mismatch() => _before.Passed.ShouldBeFalse();
    [Fact] void should_satisfy_the_unchanged_authored_expectation() => _after.Passed.ShouldBeTrue();
    [Fact] void should_preserve_the_original_destination() => ((SemanticAccepted)_after.Execution).Facts.Single().Destination.ShouldEqual(SemanticValue.Text("Old"));
    [Fact] void should_emit_the_new_source_value() => ((SemanticAccepted)_after.Execution).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("New"));
    [Fact] void should_keep_every_other_source_byte() => _result.Workspace!.Documents.Single(value => value.Id == _document.Id).Bytes.AsSpan().SequenceEqual(ExpectedBytes(_document).AsSpan()).ShouldBeTrue();
    [Fact] void should_keep_the_other_document_instance() => ReferenceEquals(_result.Workspace!.Documents.Single(value => value.Id == _concepts.Id), _concepts).ShouldBeTrue();
    [Fact] void should_keep_identity_assignments() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(_workspace.IdentityCatalog.Revision);
    [Fact] void should_change_semantic_revision() => (_result.Workspace!.Compilation.Value!.Model.Revision != _workspace.Compilation.Value!.Model.Revision).ShouldBeTrue();
    [Fact] void should_offer_one_exact_replacement() => _result.WritePlan!.Entries.Single().Kind.ShouldEqual(WorkspaceWriteKind.Replaced);
    [Fact] void should_keep_the_original_workspace() => _workspace.Documents.Single(value => value.Id == _document.Id).Bytes.ShouldEqual(_document.Bytes);
}
#endif
