// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.given;

public class compiled_models : Specification
{
    protected const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                owner String
                produces Registered
                  for projectId
                  name = name
                  owner = owner
                produces Named
                  for projectId
                  name = name
              event Registered
                name String
                owner String
              event Named
                name String
              specification Registers
                when Register
                  projectId = "11111111-1111-1111-1111-111111111111"
                  name = "First"
                  owner = "Alice"
                then Registered
                  name = "First"
                  owner = "Alice"
                then Named
                  name = "First"
            slice StateView List
              readmodel Projects
                name String
                projectId Uuid
              query All => Projects optional
                by projectId Uuid
        module Billing
          feature Invoicing
            slice StateChange Issue
              command Issue
                invoiceId Uuid identifier
                amount Decimal
                produces Issued
                  for invoiceId
                  amount = amount
              event Issued
                amount Decimal
        """;

    internal SemanticModelComparison _comparison = null!;

    protected static ExecutableSemanticModel Compile(string source, SemanticIdentityCatalog? catalog = null) =>
        Compile("Projects", [("application", "application.play", source)], catalog ?? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected static ExecutableSemanticModel Compile(string application, IEnumerable<(string StableKey, string DisplayPath, string Text)> documents, SemanticIdentityCatalog catalog)
    {
        var sources = documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile(application, SemanticDocumentSet.Create([.. sources], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        return result.Value.Model;
    }

    protected static ExecutableSemanticModel Compile(CanonicalCorpusVector vector, CanonicalCorpusSourceForm form, bool reversed = false)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => (document.StableKey, document.DisplayPath, document.Text)).ToArray();
        if (reversed)
        {
            catalog = Reversed(Workspace(vector.ApplicationName, documents, catalog).IdentityCatalog);
        }

        return Compile(vector.ApplicationName, documents, catalog);
    }

    protected static string Changed(string original, string replacement)
    {
        Assert.Contains(original, Source, StringComparison.Ordinal);
        return Source.Replace(original, replacement, StringComparison.Ordinal);
    }

    protected static ScreenplayWorkspace Workspace(string source) =>
        Workspace("Projects", [("application", "application.play", source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected static ScreenplayWorkspace Workspace(string application, IEnumerable<(string StableKey, string DisplayPath, string Text)> documents, SemanticIdentityCatalog catalog)
    {
        var workspace = ScreenplayWorkspace.Create(
            catalog.Application,
            application,
            [.. documents.Select(document => WorkspaceDocument.Create(document.StableKey, PortablePlayPath.Parse(document.DisplayPath), Encoding.UTF8.GetBytes(document.Text)))],
            catalog);
        Assert.True(workspace.Compilation.Success, string.Join('\n', workspace.Compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        return workspace;
    }

    protected static SemanticIdentityCatalog Reversed(SemanticIdentityCatalog catalog)
    {
        var ids = catalog.Semantics.Select(assignment => assignment.Id).Reverse().ToArray();
        return SemanticIdentityCatalog.Create(
            catalog.Application,
            catalog.Documents,
            [.. catalog.Semantics.Select((assignment, index) => assignment with { Id = ids[index], Origin = SemanticIdentityOrigin.Persisted })],
            catalog.EventContracts);
    }
}
