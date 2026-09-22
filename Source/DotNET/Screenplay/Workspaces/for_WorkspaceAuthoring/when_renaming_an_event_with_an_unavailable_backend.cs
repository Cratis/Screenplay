// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_renaming_an_event_with_an_unavailable_backend : for_ScreenplayWorkspace.given.a_workspace_with_external_identities
{
    WorkspaceAuthoringResult _result = null!;
    SemanticAddress _renamedEvent = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish()
    {
        Registration = Document(Registration.StableKey, Registration.Path.Value, $"import External.Unused\n{RegistrationSource}");
        Workspace = ScreenplayWorkspace.Create(StableApplicationIdentity, Workspace.ApplicationName, [Concepts, Registration], Workspace.IdentityCatalog);
        var root = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Handle.Document == Registration.Id && entry.Parent is null);
        var slice = SemanticAddress.ForSlice(StableApplicationIdentity, "Projects", "Registration", "RegisterProject");
        _renamedEvent = SemanticAddress.ForEventContract(slice, "ProjectCreated");
        var replacement = new ScreenplayCompiler().Parse($"import External.Unused\n{RegistrationSource.Replace("ProjectRegistered", "ProjectCreated", StringComparison.Ordinal)}").Value;
        var propertyRenames = Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.EventContract)
            .Select(assignment => new SemanticIdentityRename(assignment.Address, SemanticAddress.ForProperty(_renamedEvent, assignment.Address.Name)));
        _request = new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(root.Handle, root.Node, replacement)],
            SemanticRenames = [new(EventAddress, _renamedEvent), .. propertyRenames],
            EventRenames = [new(EventAddress, _renamedEvent)]
        };
    }

    void Because() => _result = Workspace.ProposeAuthoring(_request);

    [Fact] void should_accept_valid_source_without_backend_support() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_executable_readiness_false() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_preserve_the_external_event_semantic_identity() => _result.Workspace.IdentityCatalog.ResolveSemantic(_renamedEvent).ShouldEqual(EventSemanticIdentity);
    [Fact] void should_preserve_the_external_event_contract() => _result.Workspace.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Equals(_renamedEvent)).Id.ShouldEqual(EventContractIdentity);
    [Fact] void should_preserve_the_command_identity() => _result.Workspace.IdentityCatalog.ResolveSemantic(CommandAddress).ShouldEqual(CommandSemanticIdentity);
    [Fact] void should_reject_an_unexplained_event_identity_change() => Workspace.ProposeAuthoring(_request with { EventRenames = [] }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidIdentityMigration);
}
