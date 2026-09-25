// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_proposed_typed_contexts : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _stale;

    void Because()
    {
        Initialize();
        var opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var catalog = opened.GetProperty("catalogRevision").GetString();
        var documentId = Call("read-workspace", new { expectedRevision = revision }).GetProperty("result")
            .GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString();
        var syntax = new ScreenplayCompiler().Compile(Source.Replace("        produces ProjectRegistered\n          for projectId\n          projectId = projectId\n          name = name", "        handler\n          file Handler.cs\n        validate csharp\n          ```\n          return true;\n          ```", StringComparison.Ordinal)).Value!;
        var proposal = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = catalog,
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            documents = new[] { new { operation = "replace-document", documentId, node = SyntaxJson.Serialize(syntax) } }
        }).GetProperty("result").GetProperty("structuredContent");
        var id = proposal.GetProperty("proposalId").GetString();
        _first = Call("read-proposal", new { proposalId = id, view = "typed-contexts", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _second = Call("read-proposal", new { proposalId = id, view = "typed-contexts", offset = 1, limit = 1, expectedDescriptorContractRevision = "1" }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("read-proposal", new { proposalId = id, view = "typed-contexts", offset = 1, expectedDescriptorContractRevision = "2" }).GetProperty("result");
    }

    [Fact] void should_page_the_proposed_validation_context() => _first.GetProperty("result").GetProperty("page").GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("CommandValidation");
    [Fact] void should_page_the_proposed_handler_context() => _second.GetProperty("result").GetProperty("page").GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("CommandHandler");
    [Fact] void should_reject_an_unrecognized_descriptor_contract() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
