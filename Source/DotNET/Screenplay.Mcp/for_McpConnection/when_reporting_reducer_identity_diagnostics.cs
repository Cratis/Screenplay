// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_reducer_identity_diagnostics : given.a_connection
{
    JsonElement _result;
    JsonElement _scoped;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            concept ResourceId : String
            concept Month : String
            module Work
              feature Reporting
                slice StateView Totals
                  event Recorded
                  readmodel Totals
                    month Month key
                  reducer Fold => Totals
                    on Recorded
                  query ByMonth => Totals
                    by month Month
                slice StateChange Record
                  command Record
                    resource ResourceId identifier
                    produces Recorded
                      for resource
            """);
        Initialize();
    }

    void Because()
    {
        _result = Call("diagnostics", new { limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _scoped = Call("diagnostics", new { scope = "Work.Reporting.Totals", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_report_warning_without_binding_bodyless_reducer() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_page_the_native_identity_code() => _result.GetProperty("page").GetProperty("items")[0].GetProperty("code").GetString().ShouldEqual("PLAY0690");
    [Fact] void should_preserve_warning_severity() => _result.GetProperty("page").GetProperty("items")[0].GetProperty("severity").GetString().ShouldEqual("Warning");
    [Fact] void should_count_the_whole_warning() => _result.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldEqual(1);
    [Fact] void should_preserve_the_scoped_warning() => _scoped.GetProperty("page").GetProperty("items")[0].GetProperty("code").GetString().ShouldEqual("PLAY0690");
    [Fact] void should_count_scoped_warnings_before_paging() => _scoped.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldEqual(1);
}
