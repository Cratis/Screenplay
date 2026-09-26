// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_scalar_directives_with_comments : given.an_authoring_connection
{
    const string Source = """
        concept State : Enum
          // active status
          active // value note
        theme Aurora
          // compatible package
          compatible with core // compatibility note
        ui profile Desktop
          // selected platform
          target platform web // platform note
          packages
            // package name
            core // package note
          // selected theme
          theme Aurora // theme note
        module Shop
          screen template Shell
            main
          contribute to Navigation
            // contribution label
            label "Shop" // label note
            // contribution order
            order 1 // order note
          feature Ordering
            slice StateView Orders
            slice StateChange Place
              event OrderPlaced
              command Place
                id String
                // keep validation block
                validate // validation note
                  // keep rule
                  id not empty // rule note
                  // keep requirement
                  require id == "a" // requirement note
                    // keep severity
                    severity warning // severity note
                    // keep message
                    message "Denied" // message note
                produces when id == "a"
                  // keep event name
                  OrderPlaced // event note
                    // keep event target
                    for id // target note
        """;

    JsonElement _proposal;
    JsonElement _dropped;
    string _moduleSource = string.Empty;
    string _rootSource = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Shop" });
        _proposal = Result("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "module",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _dropped = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items");
        Apply(opened, _proposal);
        _moduleSource = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
        _rootSource = File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }

    [Fact] void should_keep_enum_comment() => _rootSource.ShouldContain("// active status\n  active // value note");
    [Fact] void should_keep_theme_comment() => _rootSource.ShouldContain("// compatible package\n  compatible with core // compatibility note");
    [Fact] void should_keep_profile_comment() => _rootSource.ShouldContain("// selected platform\n  target platform web // platform note");
    [Fact] void should_keep_package_comment() => _rootSource.ShouldContain("// package name\n    core // package note");
    [Fact] void should_keep_profile_theme_comment() => _rootSource.ShouldContain("// selected theme\n  theme Aurora // theme note");
    [Fact] void should_keep_contribution_label_comment() => _moduleSource.ShouldContain("// contribution label\n    label \"Shop\" // label note");
    [Fact] void should_keep_contribution_order_comment() => _moduleSource.ShouldContain("// contribution order\n    order 1 // order note");
    [Fact] void should_keep_command_subdirective_comments()
    {
        var slice = _moduleSource;
        foreach (var (comment, directive) in new[]
        {
            ("keep validation block", "validate // validation note"),
            ("keep rule", "id not empty // rule note"),
            ("keep requirement", "require id == \"a\" // requirement note"),
            ("keep severity", "severity warning // severity note"),
            ("keep message", "message \"Denied\" // message note"),
            ("keep event name", "OrderPlaced // event note"),
            ("keep event target", "for id // target note")
        })
        {
            slice.ShouldContain($"// {comment}\n");
            var lines = slice.Split('\n');
            lines.Count(line => line.Trim() == $"// {comment}").ShouldEqual(1);
            lines.Count(line => line.Trim() == directive).ShouldEqual(1);
            lines[Array.FindIndex(lines, line => line.Trim() == directive) - 1].Trim().ShouldEqual($"// {comment}");
        }
    }

    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);
}
