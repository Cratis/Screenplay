// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpStorylines;

public class when_listing_storylines : for_McpConnection.given.a_connection
{
    JsonElement _report;
    JsonElement[] _steps = [];
    JsonElement _named;
    JsonElement _unknown;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            concept ItemId : Uuid
            module Returns
              feature Requests
                slice StateChange RequestReturn
                  command RequestReturn
                    itemId ItemId
                    produces ReturnRequested
                      itemId = itemId
                  event ReturnRequested
                    itemId ItemId
              feature Refunds
                slice StateChange RequestReturn
                  command IssueRefund
                    itemId ItemId
                    produces RefundIssued
                      itemId = itemId
                  event RefundIssued
                    itemId ItemId
            storyline ReturnAnItem "A customer returns an item"
              description "From the request to the refund"
              feature Returns.Requests
              command RequestReturn "The customer asks"
              slice RequestReturn
              event ReturnApproved
            storyline Refund
              event RefundIssued
            """, new UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        _report = Call("list-storylines", new { }).GetProperty("result").GetProperty("structuredContent");
        _steps = [.. _report.GetProperty("storylines")[0].GetProperty("steps").EnumerateArray()];
        _named = Call("list-storylines", new { name = "Refund" }).GetProperty("result").GetProperty("structuredContent");
        _unknown = Call("list-storylines", new { name = "Nope" });
    }

    [Fact] void should_list_every_storyline_in_order() => _report.GetProperty("storylines").EnumerateArray().Select(storyline => storyline.GetProperty("name").GetString()).ShouldEqual(["ReturnAnItem", "Refund"]);
    [Fact] void should_carry_the_title() => _report.GetProperty("storylines")[0].GetProperty("title").GetString().ShouldEqual("A customer returns an item");
    [Fact] void should_carry_the_description() => _report.GetProperty("storylines")[0].GetProperty("description").GetString().ShouldEqual("From the request to the refund");
    [Fact] void should_list_the_steps_in_order() => _steps.Select(step => $"{step.GetProperty("kind").GetString()} {step.GetProperty("target").GetString()}").ShouldEqual(["feature Returns.Requests", "command RequestReturn", "slice RequestReturn", "event ReturnApproved"]);
    [Fact] void should_carry_a_step_note() => _steps[1].GetProperty("note").GetString().ShouldEqual("The customer asks");
    [Fact] void should_resolve_a_step_to_its_element() => (_steps[1].GetProperty("element").GetProperty("kind").GetString(), _steps[1].GetProperty("element").GetProperty("address").GetString()).ShouldEqual(("Command", "Returns.Requests.RequestReturn.RequestReturn"));
    [Fact] void should_resolve_a_feature_step() => _steps[0].GetProperty("element").GetProperty("address").GetString().ShouldEqual("Returns.Requests");
    [Fact] void should_report_an_ambiguous_step_with_its_candidates() => (_steps[2].GetProperty("status").GetString(), _steps[2].GetProperty("candidates").GetArrayLength()).ShouldEqual(("ambiguous", 2));
    [Fact] void should_report_a_missing_step() => (_steps[3].GetProperty("status").GetString(), _steps[3].GetProperty("element").ValueKind).ShouldEqual(("missing", JsonValueKind.Null));
    [Fact] void should_list_only_the_named_storyline() => _named.GetProperty("storylines").GetArrayLength().ShouldEqual(1);
    [Fact] void should_refuse_an_unknown_name() => _unknown.GetProperty("error").GetProperty("message").GetString()!.ShouldContain("Unknown storyline 'Nope'");
}
