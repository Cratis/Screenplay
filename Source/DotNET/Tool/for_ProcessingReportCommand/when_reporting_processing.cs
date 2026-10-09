// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ProcessingReportCommand;

public class when_reporting_processing : for_ModelCheck.given.a_model
{
    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), """
        purpose Billing
          description "Issue invoices"
          basis contract
        concept Name : String pii
        module M
          purpose Billing
          feature F
            slice StateChange S
              command Record
                name Name
        """);

    [Fact]
    void should_report_json_without_requiring_executable_binding()
    {
        ToolCommands.Run(["report", "processing", Root, "--format", "json", "--controller-name", "Controller"], Output, Error).ShouldEqual(0);
        using var report = JsonDocument.Parse(Output.ToString());
        report.RootElement.GetProperty("rows")[0].GetProperty("categories")[0].GetString().ShouldEqual("Name");
        report.RootElement.GetProperty("controllerName").GetString().ShouldEqual("Controller");
        report.RootElement.GetProperty("notice").GetString().ShouldContain("Not legal advice.");
        Error.ToString().ShouldBeEmpty();
    }

    [Fact]
    void should_escape_markdown_controller_text()
    {
        ProcessingReportCommand.Run(["processing", Root, "--controller-name", "Acme | **Ltd**\n<script>"], Output, Error).ShouldEqual(0);
        Output.ToString().ShouldContain("Acme \\| \\*\\*Ltd\\*\\*<br>&lt;script&gt;");
        Output.ToString().ShouldContain("Declared purposes: 1");
    }

    [Fact]
    void should_keep_csv_values_quoted_and_include_the_notice_per_row()
    {
        ProcessingReportCommand.Run(["processing", Root, "--format", "csv", "--controller-contact", "Contact, \"A\"\nSecond line"], Output, Error).ShouldEqual(0);
        Output.ToString().ShouldContain("\"Contact, \"\"A\"\"\nSecond line\"");
        Output.ToString().ShouldContain("\"Generated from declarations in this model. Not legal advice.\"");
    }

    [Theory]
    [InlineData("--format", "text")]
    [InlineData("--unknown", "x")]
    void should_refuse_unknown_formats_or_options(string option, string value) => ProcessingReportCommand.Run(["processing", Root, option, value], Output, Error).ShouldEqual(2);

    [Fact] void should_require_a_report_kind() => ProcessingReportCommand.Run([], Output, Error).ShouldEqual(2);
    [Fact] void should_require_option_values() => ProcessingReportCommand.Run(["processing", "--format"], Output, Error).ShouldEqual(2);

    [Fact]
    void should_not_publish_an_inventory_from_invalid_source()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "purpose P\n  basis invalid");
        ProcessingReportCommand.Run(["processing", Root], Output, Error).ShouldEqual(1);
        Output.ToString().ShouldBeEmpty();
    }
}
