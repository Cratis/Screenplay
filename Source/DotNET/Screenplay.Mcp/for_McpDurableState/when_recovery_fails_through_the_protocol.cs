// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_recovery_fails_through_the_protocol : given.a_durable_workspace
{
    JsonElement _response;
    string _operationId = null!;

    void Establish()
    {
        var journal = Prepare();
        _operationId = journal.Record.OperationId;
        Interrupt(journal, installState: true);
        File.WriteAllText(Path.Combine(RootPath, "renamed.play"), "external content");
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
    }

    void Because() => _response = Call("recover-workspace", new { operationId = _operationId }).GetProperty("result");

    [Fact] void should_report_a_tool_error() => _response.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_existing_status() => _response.GetProperty("structuredContent").GetProperty("status").GetString().ShouldEqual("RecoveryRequired");
    [Fact] void should_add_the_advertised_failure_discriminator() => _response.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RecoveryRequired");
    [Fact] void should_keep_the_operation_identity() => _response.GetProperty("structuredContent").GetProperty("operationId").GetString().ShouldEqual(_operationId);
    [Fact] void should_retain_the_marker() => (Files.Read(McpRecoveryJournal.FileName) is not null).ShouldBeTrue();
    [Fact] void should_leave_external_content_untouched() => File.ReadAllText(Path.Combine(RootPath, "renamed.play")).ShouldEqual("external content");

    [Fact]
    void should_match_the_documented_recovery_failure_schema()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, ".git")) && !File.Exists(Path.Combine(repository.FullName, ".git"))) repository = repository.Parent;
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository!.FullName, "Documentation/screenplay/mcp/repair-capabilities-v1.schema.json")));
        var schema = document.RootElement.GetProperty("$defs").GetProperty("recoveryFailure");
        var actual = _response.GetProperty("structuredContent");
        foreach (var required in schema.GetProperty("required").EnumerateArray())
        {
            actual.TryGetProperty(required.GetString()!, out _).ShouldBeTrue();
        }

        foreach (var property in schema.GetProperty("properties").EnumerateObject())
        {
            if (property.Value.TryGetProperty("const", out var constant)) actual.GetProperty(property.Name).GetRawText().ShouldEqual(constant.GetRawText());
        }
    }
}
