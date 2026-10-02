// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class a_proposal : given.a_host_that_renders_views
{
    string _proposalId = null!;
    JsonElement _result;

    void Establish()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        _proposalId = Call("propose", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            operation = "replace-document",
            documentId = Workspace().Documents[0].Id.ToString(),
            bytesBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Source.Replace("    slice StateChange RegisterProject", RenamedSlice.TrimStart('\r', '\n') + "\n    slice StateChange RegisterProject", StringComparison.Ordinal)))
        }).GetProperty("result").GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
    }

    void Because() => _result = Call("visualize-model", new { proposalId = _proposalId }).GetProperty("result");

    JsonElement Structured => _result.GetProperty("structuredContent");

    [Fact] void should_succeed() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_name_the_proposal() => Structured.GetProperty("proposalId").GetString().ShouldEqual(_proposalId);
    [Fact] void should_give_the_view_the_changed_document() => Structured.GetProperty("changes")[0].GetProperty("path").GetString().ShouldEqual("application.play");
    [Fact] void should_list_the_added_slice() => Structured.GetProperty("added").EnumerateArray().Select(added => added.GetString()).ShouldContain("Slice Projects.Registration.RenameProject");
    [Fact] void should_not_write_the_proposal() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
