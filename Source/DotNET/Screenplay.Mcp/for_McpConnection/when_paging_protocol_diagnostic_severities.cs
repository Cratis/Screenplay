// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_protocol_diagnostic_severities : given.a_connection
{
    JsonElement _diagnostics;
    JsonElement _allDiagnostics;
    JsonElement _executable;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            policy Access
              require authenticated
              file Policies/Access.cs
            policy Another
              require authenticated
              require role "Admin"
            module Banking
              feature Transfers
                slice StateChange Transfer
                  command Transfer
                    sourceId Uuid
                    destinationId Uuid
                    reads Account as account by sourceId
                    reads Ledger as account by destinationId
                    handler
                      file ../refused.cs
            """);
        Initialize();
    }

    void Because()
    {
        _diagnostics = Content("diagnostics", new { limit = 1 });
        _allDiagnostics = Content("diagnostics", new { });
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        _executable = Content("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" });
    }

    [Fact] void should_page_an_error_severity() => _diagnostics.GetProperty("page").GetProperty("items")[0].GetProperty("severity").GetString().ShouldEqual("Error");
    [Fact] void should_keep_full_diagnostic_totals_when_paging()
    {
        var summary = _diagnostics.GetProperty("summary");
        summary.GetProperty("errors").GetInt32().ShouldBeGreaterThan(0);
        summary.GetProperty("warnings").GetInt32().ShouldBeGreaterThan(0);
        summary.GetProperty("total").GetInt32().ShouldEqual(summary.GetProperty("errors").GetInt32() + summary.GetProperty("warnings").GetInt32() + summary.GetProperty("information").GetInt32());
    }
    [Fact] void should_keep_the_warning_severity_in_source_diagnostics() => _allDiagnostics.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.GetProperty("severity").GetString() == "Warning").ShouldBeTrue();
    [Fact] void should_include_the_duplicate_reads_alias() => Diagnostic(_allDiagnostics, "PLAY0411").GetProperty("severity").GetString().ShouldEqual("Error");
    [Fact] void should_include_mixed_and_duplicate_policy_errors_in_executable_diagnostics()
    {
        Diagnostic(_executable, "PLAY0440").GetProperty("severity").GetString().ShouldEqual("Error");
        Diagnostic(_executable, "PLAY0441").GetProperty("severity").GetString().ShouldEqual("Error");
    }
    [Fact] void should_preserve_refused_attachment_as_a_warning() => Diagnostic(_executable, "PLAY0430").GetProperty("severity").GetString().ShouldEqual("Warning");
    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
    static JsonElement Diagnostic(JsonElement result, string code) => result.GetProperty("page").GetProperty("items").EnumerateArray().First(item => item.GetProperty("code").GetString() == code);
}
