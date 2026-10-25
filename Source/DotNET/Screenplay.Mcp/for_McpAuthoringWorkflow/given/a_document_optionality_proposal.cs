// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class a_document_optionality_proposal : an_authoring_connection
{
    protected const string Types = "// café?\r\ntype Details\r\n  note    String? // keep?\r\n  lines   String[]?\r\n";
    protected JsonElement _opened;
    protected JsonElement _repair;
    protected JsonElement _proposal;
    protected byte[] _before;
    protected byte[] _preview;

    void Establish()
    {
        _before = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Types + Source)];
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _before);
        Initialize();
        _opened = Open();
        ProposeMigration();
    }

    void ProposeMigration()
    {
        var repairs = Page("repairs", _opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Where(repair => repair.GetProperty("diagnosticCode").GetString() == "PLAY0479").ToArray();
        repairs.Length.ShouldEqual(3);
        _repair = repairs.Single(repair => repair.GetProperty("scope").GetString() == "document");
        _proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0479",
            subject = _repair.GetProperty("subject"),
            formatting = "PreserveTrivia"
        });
        _preview = [.. Candidate(_proposal).Documents.Single().Bytes];
    }
}
