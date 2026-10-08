// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics_with_an_error_outside_the_filtered_document : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateChange Add\n      event Added");
        File.WriteAllText(Path.Combine(RootPath, "consumer.play"), "module N\n  feature F\n    slice StateChange Use\n      command Consume\n        id String identifier\n        produces Added\n          for id\n        description\n          ```text\nDedented body\n```\n        broken");
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "M.F.Add", document = "application.play" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_failure_despite_the_empty_page() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_count_the_error_in_the_direct_dependent() => _result.GetProperty("summary").GetProperty("errors").GetInt32().ShouldEqual(1);
    [Fact] void should_return_an_empty_filtered_page() => _result.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_the_dependent_scope() => _result.GetProperty("affectedScopes")[0].GetString().ShouldEqual("N.F.Use");
}
