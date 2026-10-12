// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

public class a_proposal_model : a_model
{
    internal string ModelSource = string.Empty;
    internal JsonElement Opened;
    internal string Document = string.Empty;

    void Establish()
    {
        ModelSource = Scenarios.Replace("name = \"Other\"", "name = \"First\"", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), ModelSource);
        Opened = Content("open-workspace", new { applicationName = "Projects" });
        Document = Content("read-workspace", new { expectedRevision = Revision, view = "documents" }).GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString()!;
    }

    internal string Revision => Opened.GetProperty("revision").GetString()!;
    internal string CatalogRevision => Opened.GetProperty("catalogRevision").GetString()!;

    internal JsonElement Content(string tool, object? arguments = null)
    {
        var response = Call(tool, arguments);
        if (response.TryGetProperty("error", out var error))
        {
            throw new McpFailure($"{tool}: {error.GetRawText()}");
        }

        return response.GetProperty("result").GetProperty("structuredContent");
    }

    internal JsonElement ProposeSource(string source)
    {
        var proposal = Content("propose-source", new
        {
            expectedRevision = Revision,
            expectedCatalogRevision = CatalogRevision,
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "replace-document", documentId = Document, source } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();

        return proposal;
    }

    internal JsonElement ApplyProposal(string proposalId) => Content("apply", new { proposalId, expectedRevision = Revision, expectedCatalogRevision = CatalogRevision });
}
