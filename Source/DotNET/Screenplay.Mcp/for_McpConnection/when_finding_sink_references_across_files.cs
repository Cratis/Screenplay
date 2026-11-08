// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_sink_references_across_files : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            sink Reporting for readmodels
            import "reports.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "reports.play"), """
            concept Customer : String
            module M
              feature F
                slice StateView S
                  readmodel Row
                    sink Reporting
                    id String key
                  readmodel Customers
                    sink Customer
                    id String key
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Reporting", kind = "Sink" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_find_the_directive_in_the_other_file() => _result.GetProperty("references").GetArrayLength().ShouldEqual(1);
    [Fact] void should_name_the_reference_role() => _result.GetProperty("references")[0].GetProperty("role").GetString().ShouldEqual("sink");
    [Fact] void should_retain_the_owning_read_model() => _result.GetProperty("references")[0].GetProperty("owner").GetProperty("address").GetString().ShouldEqual("M.F.S.Row");
}
