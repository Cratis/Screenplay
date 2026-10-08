// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_reporting_modeling_questions : given.a_model
{
    JsonElement _report;
    JsonElement[] _items = [];
    JsonElement[] _details = [];
    JsonElement _before;
    JsonElement _after;

    void Because()
    {
        _before = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
        _report = Call("find-modeling-smells", new { eventFanOutThreshold = 1, propertyFanInThreshold = 1 }).GetProperty("result").GetProperty("structuredContent");
        _items = [.. _report.GetProperty("page").GetProperty("items").EnumerateArray()];
        _details = [.. _items.Select(item => item.GetProperty("declaration")).Select(owner => Call("declaration-details", new { address = owner.GetProperty("address").GetString(), kind = owner.GetProperty("kind").GetString() }))];
        _after = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_compile_the_seed() => _report.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_all_five_rules() => _items.Select(item => item.GetProperty("ruleId").GetString()).Distinct().ShouldContainOnly("SMELL001", "SMELL002", "SMELL003", "SMELL004", "SMELL005");
    [Fact] void should_ask_questions_only() => _items.All(item => item.GetProperty("question").GetString()!.EndsWith('?')).ShouldBeTrue();
    [Fact] void should_report_information_only() => _items.All(item => item.GetProperty("severity").GetString() == "info").ShouldBeTrue();
    [Fact] void should_flag_the_generic_copy() => _items.Any(item => item.GetProperty("ruleId").GetString() == "SMELL003" && item.GetProperty("declaration").GetProperty("name").GetString() == "ProjectUpdated").ShouldBeTrue();
    [Fact] void should_name_the_fan_in_property() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SMELL005").GetProperty("property").GetString().ShouldEqual("name");
    [Fact] void should_preserve_the_entire_compile_verdict() => _after.GetRawText().ShouldEqual(_before.GetRawText());
    [Fact] void should_not_add_warnings_for_warnaserror_to_promote() => _after.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldEqual(0);
    [Fact] void should_resolve_every_finding_owner() => _details.All(detail => !detail.GetProperty("result").GetProperty("isError").GetBoolean()).ShouldBeTrue();
}
