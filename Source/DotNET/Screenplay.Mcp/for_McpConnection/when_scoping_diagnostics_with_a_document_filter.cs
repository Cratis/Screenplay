// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics_with_a_document_filter : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateChange Clean\n      event Added\n        missingType");
        File.WriteAllText(Path.Combine(RootPath, "consumer.play"), "module N\n  feature F\n    slice StateChange Use\n      command Consume\n        id String identifier\n        produces Added\n          for id\n        broken");
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "M.F.Clean", document = "application.play" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_include_only_the_selected_document() => _result.GetProperty("page").GetProperty("items").EnumerateArray().All(diagnostic => diagnostic.GetProperty("location").GetProperty("path").GetString() == "application.play").ShouldBeTrue();
    [Fact] void should_report_the_entire_scope_count() => _result.GetProperty("summary").GetProperty("total").GetInt32().ShouldEqual(2);
    [Fact] void should_report_the_filtered_page_count() => _result.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(1);
    [Fact] void should_report_the_scoped_failure() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_still_report_the_dependent_scope() => _result.GetProperty("affectedScopes")[0].GetString().ShouldEqual("N.F.Use");
}
