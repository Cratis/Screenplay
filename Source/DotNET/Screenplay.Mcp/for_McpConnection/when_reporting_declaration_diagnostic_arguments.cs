// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_declaration_diagnostic_arguments : given.a_connection
{
    const string Model = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateView List
              readmodel Project
                projectId ProjectId
                name ProjectName
              query AllProjects => Project[]
                filter tenantId String from $context.tenant
        """;

    JsonElement _subject;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
    }

    void Because()
    {
        var revision = Call("open-workspace").GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _subject = Call("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")
            .EnumerateArray().First(item => item.GetProperty("code").GetString() == "PLAY0268" && item.GetProperty("arguments").TryGetProperty("subject", out var subject) && subject.GetProperty("value").GetProperty("kind").GetString() == "query").GetProperty("arguments").GetProperty("subject");
    }

    [Fact] void should_expose_the_declaration_kind() => _subject.GetProperty("kind").GetString().ShouldEqual("declaration");
    [Fact] void should_expose_the_declaration_path() => _subject.GetProperty("value").GetProperty("path").EnumerateArray().Select(part => part.GetString()).ShouldEqual(["Projects", "Registration", "List", "AllProjects"]);
}
