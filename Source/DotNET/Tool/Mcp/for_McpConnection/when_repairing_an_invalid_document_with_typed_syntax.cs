// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_repairing_an_invalid_document_with_typed_syntax : given.a_connection
{
    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    string? _beforeId;
    string? _afterId;
    bool _unchanged;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source + "\ninvalid!\n");
        Initialize();
    }

    void Because()
    {
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        _beforeId = DocumentId(_opened);
        var syntax = new ScreenplayCompiler().Compile($"import External.Customer\n{Source}").Value;
        _proposal = Call("propose-ast", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "replace-document", documentId = _beforeId, node = SyntaxJson.Serialize(syntax) } }
        }).GetProperty("result").GetProperty("structuredContent");
        if (!_proposal.GetProperty("success").GetBoolean())
        {
            throw new McpFailure(_proposal.GetRawText());
        }

        _unchanged = File.ReadAllText(Path.Combine(RootPath, "application.play")) == Source + "\ninvalid!\n";
        _applied = Call("apply", new
        {
            proposalId = _proposal.GetProperty("proposalId").GetString(),
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
        _afterId = DocumentId(_applied.GetProperty("workspace"));
    }

    [Fact] void should_retain_the_invalid_source_on_open() => _opened.GetProperty("sourceSuccess").GetBoolean().ShouldBeFalse();
    [Fact] void should_propose_a_valid_source_replacement() => _proposal.GetProperty("after").GetProperty("sourceSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_source_until_apply() => _unchanged.ShouldBeTrue();
    [Fact] void should_apply_the_repair() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_document_identity() => _afterId.ShouldEqual(_beforeId);
    [Fact] void should_not_require_executable_backend_support() => _applied.GetProperty("workspace").GetProperty("executableReady").GetBoolean().ShouldBeFalse();

    string? DocumentId(JsonElement workspace) => Call("read-workspace", new { expectedRevision = workspace.GetProperty("revision").GetString() })
        .GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString();
}
