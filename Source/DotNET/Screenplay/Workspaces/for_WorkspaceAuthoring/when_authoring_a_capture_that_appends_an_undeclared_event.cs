// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_authoring_a_capture_that_appends_an_undeclared_event : Specification
{
    const string UndeclaredEvent =
        """
              event InvoicePaidFromSent
                invoiceId InvoiceId
                paidAt    DateTime

        """;

    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringResult _draft = null!;
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish()
    {
        var source = for_ScreenplayCompiler.given.Samples.Invoicing.Replace(UndeclaredEvent, string.Empty, StringComparison.Ordinal);
        source.ShouldNotEqual(for_ScreenplayCompiler.given.Samples.Invoicing);
        _workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Sales"), "Sales");
        var syntax = new ScreenplayCompiler().Compile(source).Value;
        _request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft,
            Documents = [new CreateWorkspaceSyntaxDocument("invoicing", PortablePlayPath.Parse("Invoicing.play"), syntax)]
        };
    }

    void Because()
    {
        _draft = _workspace.ProposeAuthoring(_request);
        _safe = _workspace.ProposeAuthoring(_request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Safe });
    }

    [Fact] void should_accept_it_as_draft_debt() => Assert.True(_draft.Accepted, string.Join(Environment.NewLine, _draft.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_disclose_the_undeclared_event() => _draft.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("InvoicePaidFromSent", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_it_in_safe_mode() => _safe.Accepted.ShouldBeFalse();
}
