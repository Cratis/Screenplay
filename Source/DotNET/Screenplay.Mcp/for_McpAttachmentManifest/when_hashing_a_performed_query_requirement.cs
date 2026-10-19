// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentManifest;

public class when_hashing_a_performed_query_requirement : Specification
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
              query CurrentFigures => InvoiceFigures optional
                by invoiceId String
                performer
                  file Queries/CurrentFigures.cs
        """;

    SemanticImplementationRequirement[] _requirements;
    string _withShape;
    string _withoutShape;

    void Establish()
    {
        var identity = ApplicationIdentity.Create("Billing");
        var catalog = SemanticIdentityCatalog.Empty(identity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application-document"), "application-document", "application.play", Source);
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        _requirements = [.. new SemanticModelBinder().Bind("Billing", syntax, SemanticDocumentSet.Create([document], catalog)).ImplementationRequirements];
    }

    void Because()
    {
        _withShape = McpAttachmentManifest.Revision(_requirements);
        _withoutShape = McpAttachmentManifest.Revision(_requirements.Select(requirement => requirement with { QueryShape = null }));
    }

    [Fact] void should_describe_the_query_shape() => _requirements.Single().QueryShape.ShouldNotBeNull();
    [Fact] void should_keep_the_manifest_revision() => _withShape.ShouldEqual(_withoutShape);
}
