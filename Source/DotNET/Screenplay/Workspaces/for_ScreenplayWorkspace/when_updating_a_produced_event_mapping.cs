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
    [Fact] void should_pass_the_authored_specification_before_the_edit() => _before.Passed.ShouldBeTrue();
    [Fact] void should_pass_the_unchanged_authored_specification_after_the_edit() => _after.Passed.ShouldBeTrue();
    [Fact] void should_preserve_the_original_destination() => ((SemanticAccepted)_after.Execution).Facts.Single().Destination.ShouldEqual(SemanticValue.Text("Same"));
    [Fact] void should_map_the_target_from_the_new_source() => ((SemanticResolvedExpression)CandidateCommand().Produces.Single().Mappings.Single().Source).Target.ShouldEqual(_operation.NewSourceCommandProperty);
    [Fact] void should_keep_every_other_source_byte() => _result.Workspace!.Documents.Single(value => value.Id == _document.Id).Bytes.AsSpan().SequenceEqual(ExpectedBytes(_document).AsSpan()).ShouldBeTrue();
    [Fact] void should_keep_the_other_document_instance() => ReferenceEquals(_result.Workspace!.Documents.Single(value => value.Id == _concepts.Id), _concepts).ShouldBeTrue();
    [Fact] void should_keep_identity_assignments() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(_workspace.IdentityCatalog.Revision);
    [Fact] void should_change_semantic_revision() => (_result.Workspace!.Compilation.Value!.Model.Revision != _workspace.Compilation.Value!.Model.Revision).ShouldBeTrue();
    [Fact] void should_offer_one_exact_replacement() => _result.WritePlan!.Entries.Single().Kind.ShouldEqual(WorkspaceWriteKind.Replaced);
    [Fact] void should_keep_the_original_workspace() => _workspace.Documents.Single(value => value.Id == _document.Id).Bytes.ShouldEqual(_document.Bytes);

    SemanticCommand CandidateCommand() => _result.Workspace!.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(value => value.Id == _command.Id);
}
#endif
