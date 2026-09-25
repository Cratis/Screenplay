// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_compilation_failed : given.an_export
{
    System.Text.Json.JsonElement _failed;
    string _continuation = null!;

    void Establish()
    {
        Initialize();
        var opened = Content("open-workspace", new { });
        _ = Content("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-model" });
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        authorize MissingPolicy");
    }

    void Because()
    {
        var opened = Content("open-workspace", new { });
        var revision = opened.GetProperty("revision").GetString();
        _failed = Content("read-workspace", new { expectedRevision = revision, view = "executable-model" });
        _continuation = Refusal(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = "previous", expectedAttachmentManifestRevision = "previous" });
    }

    [Fact] void should_be_unavailable() => _failed.GetProperty("available").GetBoolean().ShouldBeFalse();
    [Fact] void should_point_to_executable_diagnostics() => _failed.GetProperty("executableDiagnosticsView").GetString().ShouldEqual("executable-diagnostics");
    [Fact] void should_report_compilation_diagnostics() => _failed.GetProperty("executableDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
    [Fact] void should_not_return_bytes() => _failed.TryGetProperty("page", out _).ShouldBeFalse();
    [Fact] void should_not_return_last_good_revision() => _failed.TryGetProperty("modelRevision", out _).ShouldBeFalse();
    [Fact] void should_refuse_continuation() => _continuation.ShouldContain("StaleRevision");
}
