// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_reporting_missing_cases : given.a_model
{
    JsonElement _report;
    JsonElement[] _items = [];
    JsonElement[] _details = [];

    void Because()
    {
        _report = Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent");
        _items = [.. _report.GetProperty("page").GetProperty("items").EnumerateArray()];
        _details = [.. _items.Select(item => item.GetProperty("declaration")).Select(owner => Call("declaration-details", new { address = owner.GetProperty("address").GetString(), kind = owner.GetProperty("kind").GetString() }))];
    }

    [Fact] void should_compile_the_seed() => _report.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_success_separate_from_missing_refusals() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC001").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_report_every_missing_rule() => _items.Where(item => item.GetProperty("status").GetString() == "unmet").Select(item => item.GetProperty("ruleId").GetString()).ShouldContainOnly("SPEC002", "SPEC003", "SPEC003", "SPEC004", "SPEC004", "SPEC005", "SPEC006", "SPEC007", "SPEC008");
    [Fact] void should_link_the_happy_specification() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC001").GetProperty("specifications")[0].GetProperty("address").GetString().ShouldEqual("Projects.Registration.Register.Happy");
    [Fact] void should_resolve_every_obligation_owner() => _details.All(detail => !detail.GetProperty("result").GetProperty("isError").GetBoolean()).ShouldBeTrue();
}
