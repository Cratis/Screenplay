// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_selecting_completeness_checks_with_source_errors : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice Invalid Broken\n");
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { checks = "all" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_skipped_checks() => _result.GetProperty("completenessStatus").GetString()!.ShouldContain("completeness checks skipped: the model has");
    [Fact] void should_preserve_the_source_failure() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
}
