// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_revision_is_current : given.a_legacy_validation
{
    WorkspaceAuthoringResult _preview = null!;

    void Because() => _preview = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request);

    [Fact] void should_key_the_repair_by_diagnostic_code() => Repair.DiagnosticCode.ShouldEqual(DiagnosticCodes.LegacyInlineCodeFence);
    [Fact] void should_bind_the_subject_to_the_revision() => Repair.Subject.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_use_a_typed_replacement() => Repair.Operations.Single().ShouldBeOfExactType<ReplaceWorkspaceNode>();
    [Fact] void should_replace_the_subject_with_itself() => ((ReplaceWorkspaceNode)Repair.Operations.Single()).Expected.ShouldEqual(((ReplaceWorkspaceNode)Repair.Operations.Single()).Node);
    [Fact] void should_keep_the_original_bytes() => Encoding.UTF8.GetString(Workspace.Documents[0].Bytes.AsSpan()).ShouldEqual(Source);
    [Fact] void should_print_the_canonical_fence() => _preview.Workspace!.Documents[0].Text.ShouldContain("validate\n          ```csharp");
    [Fact] void should_resolve_the_warning() => WorkspaceSyntaxIndex.Create(_preview.Workspace!).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence).ShouldBeFalse();
    [Fact] void should_accept_the_preview() => _preview.Accepted.ShouldBeTrue();
    [Fact] void should_provide_a_write_plan() => _preview.WritePlan.ShouldNotBeNull();
    [Fact] void should_preserve_the_semantic_model_bytes() => SemanticModelSerializer.Serialize(_preview.Workspace!.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model)).ShouldBeTrue();
    [Fact] void should_preserve_the_requirement_id() => _preview.Workspace!.Compilation.ImplementationRequirements.Single().RequirementId.ShouldEqual(Workspace.Compilation.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_preserve_the_content_hash() => _preview.Workspace!.Compilation.ImplementationRequirements.Single().ContentHash.ShouldEqual(Workspace.Compilation.ImplementationRequirements.Single().ContentHash);
}
