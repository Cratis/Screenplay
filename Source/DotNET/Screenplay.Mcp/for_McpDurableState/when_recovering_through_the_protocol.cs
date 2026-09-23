// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_recovering_through_the_protocol : given.a_durable_workspace
{
    JsonElement _status;
    JsonElement _recovered;
    JsonElement _opened;
    JsonElement _wrongOperation;
    bool _pendingAfterWrongOperation;

    void Establish()
    {
        Interrupt(Prepare(), installState: true);
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
    }

    void Because()
    {
        _status = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
        _wrongOperation = Call("recover-workspace", new { operationId = Guid.NewGuid().ToString("N") }).GetProperty("result");
        _pendingAfterWrongOperation = Files.Read(McpRecoveryJournal.FileName) is not null;
        _recovered = Call("recover-workspace", new { operationId = _status.GetProperty("recovery").GetProperty("operationId").GetString() }).GetProperty("result");
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_report_a_pending_operation() => _status.GetProperty("recovery").GetProperty("pending").GetBoolean().ShouldBeTrue();
    [Fact] void should_refuse_an_unreviewed_operation_id() => _wrongOperation.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_marker_after_the_wrong_operation_id() => _pendingAfterWrongOperation.ShouldBeTrue();
    [Fact] void should_recover_through_tools_call() => _recovered.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_rollback() => _recovered.GetProperty("structuredContent").GetProperty("status").GetString().ShouldEqual("RolledBack");
    [Fact] void should_verify_one_original_document() => _recovered.GetProperty("structuredContent").GetProperty("verifiedDocuments").GetInt32().ShouldEqual(1);
    [Fact] void should_restore_original_source_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual([.. Original.Documents.Single().Bytes]);
    [Fact] void should_restore_original_identity_state() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_reopen_the_original_revision() => _opened.GetProperty("revision").GetString().ShouldEqual(Original.Revision.ToString());
    [Fact] void should_remove_the_verified_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
