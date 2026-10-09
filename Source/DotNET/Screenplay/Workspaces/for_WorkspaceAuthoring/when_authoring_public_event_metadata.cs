// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_authoring_public_event_metadata : Specification
{
    const string Source = "module Shipping\n  feature Orders\n    slice Translate Transfer\n      event Shipped\n        name String\n";
    ScreenplayWorkspace _workspace;
    WorkspaceDocument _document;
    ApplicationSyntax _syntax;

    void Establish()
    {
        _document = WorkspaceDocument.Create("shipping", PortablePlayPath.Parse("shipping.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("Shipping", [_document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shipping")));
        _syntax = new ScreenplayCompiler().Parse(Source).Value!;
    }

    [Fact]
    void should_accept_authoring_as_executable_at_the_claimed_version()
    {
        var module = _syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var changed = _syntax with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with
            {
                Direction = TranslationDirection.Inbound,
                Events = [slice.Events.Single() with { Visibility = EventVisibility.Public, Origin = "../not-a-file/*.play" }]
            }] }] }]
        };
        var request = Request(changed);
        var authoring = _workspace.ProposeAuthoring(request);
        Assert.True(authoring.Accepted, string.Join("; ", authoring.Conflicts.Select(conflict => conflict.Message)));
        authoring.ExecutableReady.ShouldBeTrue();
        authoring.Workspace!.Compilation.Diagnostics.Any(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
        authoring.Workspace.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(_workspace.IdentityCatalog.EventContracts.Single().Id);
        _workspace.ProposeAuthoring(request with { Validation = WorkspaceAuthoringValidation.Executable }).Accepted.ShouldBeTrue();
    }

    [Fact]
    void should_reject_a_direction_on_another_slice_kind()
    {
        var module = _syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single() with { Type = SliceType.Automation, Direction = TranslationDirection.Inbound };
        var changed = _syntax with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        _workspace.ProposeAuthoring(Request(changed)).Accepted.ShouldBeFalse();
        _workspace.Documents.Single().Text.ShouldEqual(Source);
    }

    WorkspaceAuthoringRequest Request(ApplicationSyntax syntax) => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Documents = [new ReplaceWorkspaceSyntaxDocument(_document.Id, syntax)]
    };
}
