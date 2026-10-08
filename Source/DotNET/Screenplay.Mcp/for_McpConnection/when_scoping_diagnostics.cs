// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module M
              feature F
                slice StateChange Clean
                  event Added
                    value String
                slice StateChange Broken
                  event Other
                    value
            module N
              feature F
                slice StateChange Consumer
                  command Consume
                    id String identifier
                    value String
                    produces Added
                      for id
                      value = value
            """);
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "M.F.Clean", limit = 1 }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_a_clean_scoped_check() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_whole_application_status() => _result.GetProperty("wholeApplicationSuccess").GetBoolean().ShouldBeFalse();
    [Fact] void should_filter_diagnostic_pages() => _result.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
    [Fact] void should_filter_summary_counts() => _result.GetProperty("summary").GetProperty("total").GetInt32().ShouldEqual(0);
    [Fact] void should_count_scoped_declarations() => _result.GetProperty("declarationCount").GetInt32().ShouldEqual(2);
    [Fact] void should_count_the_dependent_command() => _result.GetProperty("dependentDeclarationCount").GetInt32().ShouldEqual(1);
    [Fact] void should_identify_the_affected_scope() => _result.GetProperty("affectedScopes")[0].GetString().ShouldEqual("N.F.Consumer");
}
