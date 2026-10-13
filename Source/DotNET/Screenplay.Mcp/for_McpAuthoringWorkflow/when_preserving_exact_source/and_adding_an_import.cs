// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_adding_an_import : given.an_exact_source_connection
{
    JsonElement _proposal;
    bool _unchangedBeforeApply;

    void Because()
    {
        _proposal = Result("propose-source", Arguments(ReplaceSource(WithImport), CreateNotifications()));
        _unchangedBeforeApply = File.ReadAllText(ApplicationPath) == Original;
        Apply(Opened, _proposal);
    }

    [Fact] void should_accept_the_structural_edit() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_leave_disk_unchanged_until_apply() => _unchangedBeforeApply.ShouldBeTrue();
    [Fact] void should_keep_every_supplied_replacement_byte() => File.ReadAllBytes(ApplicationPath).SequenceEqual(Encoding.UTF8.GetBytes(WithImport)).ShouldBeTrue();
    [Fact] void should_keep_every_supplied_creation_byte() => File.ReadAllBytes(Path.Combine(RootPath, "Notifications/Notifications.play")).SequenceEqual(Encoding.UTF8.GetBytes(Notifications)).ShouldBeTrue();
    [Fact] void should_drop_no_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_report_normalization() => _proposal.GetProperty("canonicalizedSource").GetBoolean().ShouldBeFalse();
    [Fact] void should_emit_no_normalization_diagnostic() => _proposal.GetProperty("authoringDiagnosticSeverities").GetProperty("codes").EnumerateArray().Select(code => code.GetString()).ShouldNotContain("PLAY0288");
}
