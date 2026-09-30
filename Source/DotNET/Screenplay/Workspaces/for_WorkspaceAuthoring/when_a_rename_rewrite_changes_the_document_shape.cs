// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_a_rename_rewrite_changes_the_document_shape : Specification
{
    const string Source =
        """
        type InvoiceKey
          id String
        """;

    Exception _error = null!;

    static WorkspaceSyntaxIndex Index(string source)
    {
        var document = WorkspaceDocument.Create("invoice", PortablePlayPath.Parse("Billing/Invoices.play"), Encoding.UTF8.GetBytes(source));
        return WorkspaceSyntaxIndex.Create(ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"))));
    }

    void Because()
    {
        var before = Index(Source);
        var after = Index($"{Source}\n  code String");
        _error = Catch.Exception(() => WorkspaceAuthoringTransaction.RequireShape(before, after, before.Entries.First().Handle.Document));
    }

    [Fact] void should_refuse_the_rewrite() => _error.ShouldBeOfExactType<InvalidWorkspaceAuthoring>();
    [Fact] void should_explain_the_unproven_correspondence() => _error.Message.ShouldContain("changed the syntax shape of a document");
}
