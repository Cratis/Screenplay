// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Agents.given;

public class a_manifest_model : an_agent_model
{
    protected static string BindableModel => "concept InvoiceId : Uuid\n" + Model.Replace("        id String", "        invoiceId InvoiceId", StringComparison.Ordinal);

    protected static CompilationResult<SemanticCompilation> Bind(string source)
    {
        var identities = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Agents"));
        var document = SemanticSourceDocument.Create(identities.ResolveDocument("application"), "application", "application.play", source);
        return new SemanticModelCompiler().Compile("Agents", SemanticDocumentSet.Create([document], identities));
    }

    protected static string DocumentationFile(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Screenplay.slnx"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Documentation", "screenplay", name);
    }
}
