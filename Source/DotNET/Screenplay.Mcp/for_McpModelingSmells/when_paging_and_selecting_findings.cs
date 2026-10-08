// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_paging_and_selecting_findings : given.a_model
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _scoped;
    JsonElement _unknown;
    JsonElement _missingRevision;
    JsonElement _stale;
    JsonElement _threshold;
    JsonElement _wrongDocument;
    JsonElement _invalidThreshold;

    void Because()
    {
        _first = Call("find-modeling-smells", new { limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var revision = _first.GetProperty("sourceRevision").GetString();
        _next = Call("find-modeling-smells", new { offset = 1, limit = 1, expectedSourceRevision = revision }).GetProperty("result").GetProperty("structuredContent");
        _scoped = Call("find-modeling-smells", new { scope = "Projects.Maintenance.Register" }).GetProperty("result").GetProperty("structuredContent");
        _unknown = Call("find-modeling-smells", new { scope = "Missing" });
        _missingRevision = Call("find-modeling-smells", new { offset = 1 });
        _wrongDocument = Call("find-modeling-smells", new { document = "missing.play" }).GetProperty("result").GetProperty("structuredContent");
        _threshold = Call("find-modeling-smells", new { eventFanOutThreshold = 2, propertyFanInThreshold = 2 }).GetProperty("result").GetProperty("structuredContent");
        _invalidThreshold = Call("find-modeling-smells", new { eventFanOutThreshold = 0 });
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// changed\n");
        _stale = Call("find-modeling-smells", new { expectedSourceRevision = revision });
    }

    [Fact] void should_bound_the_page() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_offer_continuation() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_keep_the_revision() => _next.GetProperty("sourceRevision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_retain_cross_scope_duplicate_evidence() => _scoped.GetProperty("page").GetProperty("items")[0].GetProperty("related")[0].GetProperty("name").GetString().ShouldEqual("ProjectUpdated");
    [Fact] void should_filter_by_document() => _wrongDocument.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_flag_counts_equal_to_thresholds() => _threshold.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.GetProperty("ruleId").GetString() == "SMELL004" || item.GetProperty("ruleId").GetString() == "SMELL005").ShouldBeFalse();
    [Fact] void should_reject_an_unknown_scope() => _unknown.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_require_a_revision_on_continuation() => _missingRevision.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_stale_source() => _stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_reject_a_zero_threshold() => _invalidThreshold.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
}
