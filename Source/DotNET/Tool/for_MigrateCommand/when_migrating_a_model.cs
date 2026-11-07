// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_migrating_a_model : given.a_screenplay_5_model
{
    int _exitCode;
    JsonElement _report;

    void Because()
    {
        var report = Path.Combine(Root, "migration.json");
        _exitCode = ToolCommands.Run(["migrate", Document, "--from", "5", "--to", "6", "--report", report], Output, Error);
        using var document = JsonDocument.Parse(File.ReadAllText(report));
        _report = document.RootElement.Clone();
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_write_the_screenplay_6_form() => File.ReadAllText(Document).ShouldContain("  navigation\n  outlet Navigation of navigation in navigation\n");
    [Fact] void should_keep_the_comment() => File.ReadAllText(Document).ShouldContain("  // Modules add their destinations here.\n  navigation\n");
    [Fact] void should_report_the_outcome() => _report.GetProperty("outcome").GetString().ShouldEqual("migrated");
    [Fact] void should_report_the_steps() => _report.GetProperty("steps")[0].GetProperty("name").GetString().ShouldEqual("slots-and-outlets");
    [Fact] void should_report_the_file_as_written() => _report.GetProperty("files")[0].GetProperty("written").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_change_line() => _report.GetProperty("files")[0].GetProperty("changes")[0].GetProperty("line").GetInt32().ShouldEqual(4);
}
