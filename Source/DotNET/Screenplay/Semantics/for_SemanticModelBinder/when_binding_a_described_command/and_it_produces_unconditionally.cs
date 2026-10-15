// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_described_command;

public class and_it_produces_unconditionally : given.a_semantic_binder
{
    internal const string Source = "module M\n  feature F\n    slice StateChange S\n      event Chosen\n        value String\n      command Choose\n        handler\n          implementation\n            hint \"First hint\"\n            hint \"Second hint\"\n        produces Chosen\n";
    CompilationResult<SemanticCompilation> _result;
    SemanticCommand _command;
    SemanticImplementationRequirement _requirement;

    void Because()
    {
        _result = Bind(Source);
        Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _command = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        _requirement = _result.ImplementationRequirements.Single();
    }

    [Fact] void should_bind_unmapped_required_payloads() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_v23() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_link_the_canonical_command() => _command.Handler!.RequirementId.ShouldEqual(_requirement.RequirementId);
    [Fact] void should_name_the_pending_requirement() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.DescribedCommandNotImplemented && diagnostic.Severity == DiagnosticSeverity.Information && diagnostic.Message.Contains(_requirement.RequirementId, StringComparison.Ordinal));
    [Fact] void should_keep_ordered_hints() => _requirement.Hints.SequenceEqual(["First hint", "Second hint"]).ShouldBeTrue();
    [Fact] void should_be_pending() => _requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Pending);
    [Fact] void should_use_contract_v1() => (_requirement.ContextVersion, _requirement.ResultVersion, _command.Handler!.ResultVersion).ShouldEqual((1u, 1u, 1u));
    [Fact] void should_disclose_provisional_identity() => _requirement.IsProvisional.ShouldBeTrue();
    [Fact] void should_not_be_ambiguous() => _requirement.IsAmbiguous.ShouldBeFalse();
    [Fact] void should_not_claim_a_body() => (_requirement.File, _requirement.Language, _requirement.BodySpan).ShouldEqual((null, null, null));
    [Fact] void should_not_claim_content() => _requirement.ContentHash.ShouldEqual(string.Empty);
    [Fact] void should_emit_the_command_context() => _result.TypedContextDescriptors.Single().RequirementId.ShouldEqual(_requirement.RequirementId);
    [Fact] void should_match_the_authoring_inventory()
    {
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(_applicationIdentity));
        WorkspaceImplementationInventory.Create(workspace).Entries.Single().RequirementId.ShouldEqual(_requirement.RequirementId);
    }
    [Fact] void should_choose_v23_over_atomic_batches() => Bind("reaction batches atomic\n" + Source).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact]
    void should_disclose_persisted_identity()
    {
        var catalog = SemanticIdentityCatalog.Create(_applicationIdentity, [], [new(_requirement.Owner, _requirement.Source.SemanticId, SemanticIdentityOrigin.Persisted)], []);
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))], catalog);
        workspace.Compilation.Success.ShouldBeTrue();
        workspace.Compilation.ImplementationRequirements.Single().IsProvisional.ShouldBeFalse();
    }
}
