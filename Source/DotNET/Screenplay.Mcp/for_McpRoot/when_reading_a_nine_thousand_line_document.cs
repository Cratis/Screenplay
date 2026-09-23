// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_reading_a_nine_thousand_line_document : for_McpConnection.given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        var comments = string.Join('\n', Enumerable.Repeat("// Business rules and explanatory context for the application", 9000));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), $"{comments}\n{Source}");
        Initialize();
    }

    void Because() => _result = Call("diagnostics").GetProperty("result");

    [Fact] void should_accept_a_large_single_file() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_compile_the_whole_document() => _result.GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_one_source_file() => _result.GetProperty("structuredContent").GetProperty("fileCount").GetInt32().ShouldEqual(1);
}
