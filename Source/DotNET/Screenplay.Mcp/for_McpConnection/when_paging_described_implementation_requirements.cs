// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_described_implementation_requirements : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _stale;

    void Because()
    {
        var source = given.described_commands.Source + "\n    slice StateChange OtherFee\n      command OtherFee\n        handler\n          implementation\n            hint \"Choose the other fee\"\n        produces OtherFeeApplied\n      event OtherFeeApplied\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var revision = Call("open-workspace", new { applicationName = "Billing" }).GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _first = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _second = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", offset = 1, limit = 1, expectedAttachmentManifestRevision = _first.GetProperty("attachmentManifestRevision").GetString() }).GetProperty("result").GetProperty("structuredContent");
        _stale = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = "stale" }).GetProperty("result");
    }

    [Fact] void should_count_both_requirements() => _first.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(2);
    [Fact] void should_bound_each_page() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_continue_to_a_distinct_identity() => _second.GetProperty("page").GetProperty("items")[0].GetProperty("requirementId").GetString().ShouldNotEqual(_first.GetProperty("page").GetProperty("items")[0].GetProperty("requirementId").GetString());
    [Fact] void should_report_pending_on_the_second_page() => _second.GetProperty("page").GetProperty("items")[0].GetProperty("attachmentResolution").GetString().ShouldEqual("Pending");
    [Fact] void should_refuse_a_stale_manifest() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
