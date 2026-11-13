// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_authoring_a_storyline : given.an_authoring_connection
{
    string _added = string.Empty;
    string _reordered = string.Empty;
    JsonElement _listed;
    JsonElement _unresolved;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Open();
        var application = Node("ApplicationSyntax", opened.GetProperty("revision").GetString()!);
        _added = ApplyChange(opened, new
        {
            operation = "add",
            parent = application.GetProperty("handle"),
            member = "storylines",
            node = new JsonObject
            {
                ["kind"] = "StorylineSyntax",
                ["name"] = "RegisterAndList",
                ["title"] = "A project is registered and listed",
                ["steps"] = new JsonArray(Step("Command", "RegisterProject", "Register it"), Step("Event", "ProjectRegistered", null), Step("Slice", "RegisterProject", null))
            }
        });

        opened = Open();
        var slice = Node("StorylineStepSyntax", opened.GetProperty("revision").GetString()!, 2);
        var storyline = Node("StorylineSyntax", opened.GetProperty("revision").GetString()!);
        _reordered = ApplyChange(opened, new { operation = "move", target = slice.GetProperty("handle"), parent = storyline.GetProperty("handle"), member = "steps", index = 0 });
        _listed = Result("list-storylines");

        opened = Open();
        storyline = Node("StorylineSyntax", opened.GetProperty("revision").GetString()!);
        _unresolved = Call("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "add", parent = storyline.GetProperty("handle"), member = "steps", node = Step("Screen", "ProjectList", null) } }
        }).GetProperty("result");
    }

    [Fact] void should_print_the_new_storyline() => _added.ShouldContain("storyline RegisterAndList \"A project is registered and listed\"\n  command RegisterProject \"Register it\"\n  event ProjectRegistered\n  slice RegisterProject\n");
    [Fact] void should_reorder_the_steps() => _reordered.ShouldContain("storyline RegisterAndList \"A project is registered and listed\"\n  slice RegisterProject\n  command RegisterProject \"Register it\"\n  event ProjectRegistered\n");
    [Fact] void should_refuse_a_step_that_names_nothing() => _unresolved.GetProperty("structuredContent").GetProperty("conflicts")[0].GetProperty("message").GetString().ShouldContain("unresolved Screen reference 'ProjectList'");
    [Fact] void should_resolve_every_step() => _listed.GetProperty("storylines")[0].GetProperty("steps").EnumerateArray().All(step => step.GetProperty("status").GetString() == "resolved").ShouldBeTrue();

    static JsonObject Step(string kind, string target, string? note) => new()
    {
        ["kind"] = "StorylineStepSyntax",
        ["elementKind"] = kind,
        ["target"] = target,
        ["note"] = note
    };

    JsonElement Node(string kind, string revision, int index) =>
        Result("read-ast", new { expectedRevision = revision, kind, includeContent = true }).GetProperty("page").GetProperty("items")[index];

    string ApplyChange(JsonElement opened, object operation)
    {
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { operation }
        });
        Apply(opened, proposal);
        return File.ReadAllText(Path.Combine(RootPath, "application.play")).ReplaceLineEndings("\n");
    }
}
