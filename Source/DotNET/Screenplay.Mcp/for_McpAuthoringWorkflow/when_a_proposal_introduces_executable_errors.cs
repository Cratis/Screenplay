// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_a_proposal_introduces_executable_errors : given.an_authoring_connection
{
    const string ModelWithAnExistingError = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a new project"
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
            slice StateView List
              readmodel Project
                projectId ProjectId
                name ProjectName
              query GetProjects => Project[]
                filter tenantId String from $context.tenant
        """;

    JsonElement _unrelated;
    JsonElement _introducing;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), ModelWithAnExistingError, new UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var catalog = opened.GetProperty("catalogRevision").GetString()!;
        var slices = Result("read-ast", new { expectedRevision = revision, kind = "SliceSyntax", includeContent = true }).GetProperty("page").GetProperty("items").EnumerateArray().ToArray();

        var registration = slices.Single(slice => slice.GetProperty("name").GetString() == "RegisterProject");
        var described = JsonNode.Parse(registration.GetProperty("node").GetRawText())!;
        described["description"] = "Registers a project the team will work on";
        _unrelated = Result("propose-ast", Replace(revision, catalog, registration, described));

        var list = slices.Single(slice => slice.GetProperty("name").GetString() == "List");
        var withSecondList = JsonNode.Parse(list.GetProperty("node").GetRawText())!;
        var second = withSecondList["queries"]![0]!.DeepClone();
        second["name"] = "AllProjects";
        withSecondList["queries"]!.AsArray().Add(second);
        _introducing = Result("propose-ast", Replace(revision, catalog, list, withSecondList));
    }

    [Fact] void should_not_repeat_errors_the_model_already_had() => _unrelated.GetProperty("introducedExecutableErrors").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_give_guidance_without_new_errors() => _unrelated.GetProperty("executableGuidance").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_list_the_new_error() => _introducing.GetProperty("introducedExecutableErrors").EnumerateArray().Single().GetProperty("message").GetString()!.ShouldContain("'AllProjects'");
    [Fact] void should_give_the_code_of_the_new_error() => _introducing.GetProperty("introducedExecutableErrors")[0].GetProperty("code").GetString().ShouldEqual("PLAY0268");
    [Fact] void should_say_to_fix_it_before_apply() => _introducing.GetProperty("executableGuidance").GetString()!.ShouldContain("before apply");

    static object Replace(string revision, string catalog, JsonElement target, JsonNode node) => new
    {
        expectedRevision = revision,
        expectedCatalogRevision = catalog,
        formatting = "CanonicalizeTouchedDocuments",
        operations = new[] { new { operation = "replace", target = target.GetProperty("handle"), node } }
    };
}
