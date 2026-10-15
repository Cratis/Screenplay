// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_described_handler_intents : given.a_connection
{
    JsonElement _handler;
    JsonElement _requirement;
    JsonElement _hints;
    JsonElement _summary;
    JsonElement _refused;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), given.described_commands.Source);
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Billing" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var intents = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _handler = intents.GetProperty("page").GetProperty("items")[0];
        var id = _handler.GetProperty("requirementId").GetString();
        _hints = Call("read-workspace", new { expectedRevision = revision, view = "handler-intent-details", requirementId = id, limit = 1 }).GetProperty("result").GetProperty("structuredContent").GetProperty("page");
        _requirement = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 1 }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0];
        _summary = Call("describe-application").GetProperty("result").GetProperty("structuredContent");
        File.WriteAllText(Path.Combine(RootPath, "application.play"), given.described_commands.Source.Replace("        produces LateFeeApplied\n          for invoiceId\n", string.Empty, StringComparison.Ordinal));
        revision = Call("open-workspace", new { applicationName = "Billing" }).GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _refused = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0];
    }

    [Fact] void should_report_admitted_described_intent() => _handler.GetProperty("admission").GetString().ShouldEqual("described");
    [Fact] void should_report_executable_admission_not_body_execution() => _handler.GetProperty("executableReady").GetBoolean().ShouldBeTrue();
    [Fact] void should_pair_the_inventory_and_sidecar_identity() => _requirement.GetProperty("requirementId").GetString().ShouldEqual(_handler.GetProperty("requirementId").GetString());
    [Fact] void should_report_pending_attachment() => _requirement.GetProperty("attachmentResolution").GetString().ShouldEqual("Pending");
    [Fact] void should_retain_ordered_hints() => _requirement.GetProperty("hints").EnumerateArray().Select(hint => hint.GetString()).ShouldEqual(["Calculate the fee from the overdue days", "Round to the nearest whole amount"]);
    [Fact] void should_count_hints() => _requirement.GetProperty("hintCount").GetInt32().ShouldEqual(2);
    [Fact] void should_disclose_provisional_identity() => _requirement.GetProperty("provisional").GetBoolean().ShouldBeTrue();
    [Fact] void should_never_claim_ambiguity_after_binding() => _requirement.GetProperty("ambiguous").GetBoolean().ShouldBeFalse();
    [Fact] void should_emit_the_handler_descriptor() => _requirement.GetProperty("typedContext").GetProperty("count").GetInt32().ShouldEqual(1);
    [Fact] void should_page_hints_separately() => _hints.GetProperty("items")[0].GetString().ShouldEqual("Calculate the fee from the overdue days");
    [Fact] void should_report_described_count() => _summary.GetProperty("describedCommandCount").GetInt32().ShouldEqual(1);
    [Fact] void should_explain_body_readiness() => _summary.GetProperty("describedCommandReadiness").GetString().ShouldContain("ImplementationBody");
    [Fact] void should_not_admit_productionless_intents() => _refused.GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_disclose_refused_intents() => _refused.GetProperty("admission").GetString().ShouldEqual("refused");
}
