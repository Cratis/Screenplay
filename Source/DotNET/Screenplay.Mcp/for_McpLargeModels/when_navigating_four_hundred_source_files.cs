// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_navigating_four_hundred_source_files : for_McpConnection.given.a_connection
{
    JsonElement _summary;
    JsonElement _modules;
    JsonElement _features;
    JsonElement _slices;
    JsonElement _commands;
    JsonElement _properties;
    JsonElement _incoming;
    JsonElement _outgoing;
    JsonElement _fixtures;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        for (var number = 0; number < 400; number++)
        {
            var module = $"M{number / 100}";
            var decade = number / 10;
            var feature = $"F{decade % 10}";
            var folder = Path.Combine(RootPath, module, feature);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, $"S{number}.play"), $$"""
                module {{module}}
                  feature {{feature}}
                    slice StateChange S{{number}}
                      command Create{{number}}
                        title String
                        produces Created{{number}}
                          title = title
                      event Created{{number}}
                        title String
                      specification Valid
                        when Create{{number}}
                          title = "example"
                        then Created{{number}}
                          title = "example"
                """);
        }

        Initialize();
    }

    void Because()
    {
        _summary = Read("describe-application");
        _modules = Read("describe-application", new { view = "children", limit = 2 });
        _features = Read("describe-application", new { view = "children", parent = "M0" });
        _slices = Read("describe-application", new { view = "children", parent = "M0.F0" });
        _commands = Read("search-declarations", new { kind = "Command", scope = "M0.F0", name = "Create", match = "prefix" });
        _properties = Read("declaration-details", new { address = "M0.F0.S0.Create0", kind = "Command", view = "properties" });
        _incoming = Read("dependencies", new { address = "M0.F0.S0.Created0", kind = "Event" });
        _outgoing = Read("dependencies", new { address = "M0.F0.S0.Create0", kind = "Command", direction = "outgoing" });
        _fixtures = Read("find-fixtures", new { scope = "M0.F0", property = "title", value = "example" });
    }

    [Fact] void should_read_all_four_hundred_files() => _summary.GetProperty("fileCount").GetInt32().ShouldEqual(400);
    [Fact] void should_report_a_valid_whole_model() => _summary.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_four_logical_modules() => _summary.GetProperty("moduleCount").GetInt32().ShouldEqual(4);
    [Fact] void should_report_forty_logical_features() => _summary.GetProperty("featureCount").GetInt32().ShouldEqual(40);
    [Fact] void should_report_four_hundred_slices() => _summary.GetProperty("sliceCount").GetInt32().ShouldEqual(400);
    [Fact] void should_page_modules_without_the_entire_hierarchy() => _modules.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    [Fact] void should_offer_the_next_module_page() => _modules.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(2);
    [Fact] void should_navigate_into_a_module() => _features.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(10);
    [Fact] void should_navigate_into_a_feature() => _slices.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(10);
    [Fact] void should_scope_declaration_search() => _commands.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(10);
    [Fact] void should_not_return_command_ast_subtrees() => _commands.GetProperty("page").GetProperty("items")[0].TryGetProperty("syntax", out _).ShouldBeFalse();
    [Fact] void should_inspect_one_property() => _properties.GetProperty("details").GetProperty("items")[0].GetProperty("name").GetString().ShouldEqual("title");
    [Fact] void should_find_direct_event_impact() => _incoming.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    [Fact] void should_find_direct_command_dependencies() => _outgoing.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_scope_fixture_occurrences() => _fixtures.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(20);

    JsonElement Read(string tool, object? arguments = null)
    {
        var response = Call(tool, arguments).GetProperty("result");
        if (response.GetProperty("isError").GetBoolean())
        {
            throw new McpFailure(response.GetRawText());
        }

        return response.GetProperty("structuredContent");
    }
}
