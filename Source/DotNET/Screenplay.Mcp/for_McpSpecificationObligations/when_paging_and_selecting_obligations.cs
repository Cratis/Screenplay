// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_paging_and_selecting_obligations : given.a_model
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _scoped;
    JsonElement _unknown;
    JsonElement _missingRevision;
    JsonElement _stale;
    JsonElement _wrongDocument;

    void Because()
    {
        _first = Call("find-specification-obligations", new { limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var revision = _first.GetProperty("sourceRevision").GetString();
        _next = Call("find-specification-obligations", new { offset = 1, limit = 1, expectedSourceRevision = revision }).GetProperty("result").GetProperty("structuredContent");
        _scoped = Call("find-specification-obligations", new { scope = "Projects.Registration.Register" }).GetProperty("result").GetProperty("structuredContent");
        _unknown = Call("find-specification-obligations", new { scope = "Missing" });
        _missingRevision = Call("find-specification-obligations", new { offset = 1 });
        _wrongDocument = Call("find-specification-obligations", new { document = "missing.play" }).GetProperty("result").GetProperty("structuredContent");
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// changed\n");
        _stale = Call("find-specification-obligations", new { expectedSourceRevision = revision });
    }

    [Fact] void should_bound_the_page() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_offer_continuation() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_continue_without_repeating() => _next.GetProperty("page").GetProperty("items")[0].GetProperty("ruleId").GetString().ShouldNotEqual(_first.GetProperty("page").GetProperty("items")[0].GetProperty("ruleId").GetString());
    [Fact] void should_include_referenced_concept_rules_in_scope() => _scoped.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.GetProperty("declaration").GetProperty("kind").GetString() == "Concept").ShouldBeTrue();
    [Fact] void should_filter_by_document() => _wrongDocument.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_reject_an_unknown_scope() => _unknown.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_require_a_revision_on_continuation() => _missingRevision.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_stale_source() => _stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
}
