// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_declaration_diagnostic_arguments : given.a_connection
{
    JsonElement _subject;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept Code : String\n  validate\n    matches \"[\"");
        Initialize();
    }

    void Because() => _subject = Call("diagnostics").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")
        .EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0367").GetProperty("arguments").GetProperty("subject");

    [Fact] void should_expose_the_declaration_kind() => _subject.GetProperty("kind").GetString().ShouldEqual("declaration");
    [Fact] void should_expose_the_declaration_path() => _subject.GetProperty("value").GetProperty("path").EnumerateArray().Select(part => part.GetString()).ShouldEqual(["Code"]);
}
