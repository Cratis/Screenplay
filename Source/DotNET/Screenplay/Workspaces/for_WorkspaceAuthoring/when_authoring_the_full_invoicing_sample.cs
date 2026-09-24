// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_authoring_the_full_invoicing_sample : Specification
{
    ScreenplayWorkspace _workspace = null!;
    ApplicationSyntax _syntax = null!;
    WorkspaceAuthoringResult _result = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish()
    {
        _workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Sales"), "Sales");
        _syntax = new ScreenplayCompiler().Compile(for_ScreenplayCompiler.given.Samples.Invoicing).Value;
    }

    void Because()
    {
        _request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft,
            Documents = [new CreateWorkspaceSyntaxDocument("invoicing", PortablePlayPath.Parse("Invoicing.play"), _syntax)]
        };
        _result = _workspace.ProposeAuthoring(_request);
    }

    [Fact] void should_accept_the_complete_language_showcase() => Assert.True(_result.Accepted, string.Join(Environment.NewLine, _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_accept_the_showcase_in_safe_mode_too() => _workspace.ProposeAuthoring(_request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Safe }).Accepted.ShouldBeTrue();
    [Fact] void should_carry_no_reference_debt() => _result.AuthoringDiagnostics.Where(diagnostic => diagnostic.Message.StartsWith("Reference debt", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
    [Fact] void should_keep_executable_readiness_distinct() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_preserve_every_structural_value() => SyntaxJson.StructurallyEqual(_syntax, WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Single(entry => entry.Parent is null).Node).ShouldBeTrue();
    [Fact] void should_not_invent_semantic_identities_without_backend_support() => _result.Workspace.IdentityCatalog.Semantics.ShouldBeEmpty();
    [Fact] void should_not_invent_event_contracts_without_backend_support() => _result.Workspace.IdentityCatalog.EventContracts.ShouldBeEmpty();
    [Fact] void should_assign_the_exact_document_identity() => _result.Workspace.IdentityCatalog.Documents.Single().Id.ShouldEqual(_result.Workspace.Documents.Single().Id);
}
