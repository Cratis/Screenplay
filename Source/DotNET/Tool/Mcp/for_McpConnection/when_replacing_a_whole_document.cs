// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_replacing_a_whole_document : given.a_connection
{
    JsonElement _result;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        _result = Call("propose", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            operation = "replace-document",
            documentId = Workspace().Documents[0].Id.ToString(),
            bytesBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Source.Replace("Registers a new project", "Registers the project", StringComparison.Ordinal)))
        }).GetProperty("result");
    }

    [Fact] void should_admit_the_revision_bound_document_replacement() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_exact_candidate_source() => _result.GetProperty("structuredContent").GetProperty("changes")[0].GetProperty("after").GetProperty("text").GetString()!.Contains("Registers the project", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_write_during_proposal() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
