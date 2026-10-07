// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class a_proposal_that_renames_a_reaction : given.a_host_that_renders_views
{
    string _proposalId = null!;
    JsonElement _result;

    void Establish()
    {
        const string source = Source + "\n" + """
            slice Automation NotifyProject
              reaction ProjectNotifier
                when ProjectRegistered
                  produces ProjectNotificationSent
                    for projectId
              event ProjectNotificationSent
        """;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var slice = SemanticAddress.ForSlice(Workspace().IdentityCatalog.Application, "Projects", ["Registration"], "NotifyProject");
        var previousAddress = JsonSerializer.SerializeToElement(McpSemanticAddresses.Describe(SemanticAddress.ForReaction(slice, "ProjectNotifier")), McpJson.Options);
        var currentAddress = JsonSerializer.SerializeToElement(McpSemanticAddresses.Describe(SemanticAddress.ForReaction(slice, "RegistrationNotifier")), McpJson.Options);
        var proposal = Call("propose", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            operation = "replace-document",
            documentId = Workspace().Documents[0].Id.ToString(),
            bytesBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(source.Replace("ProjectNotifier", "RegistrationNotifier", StringComparison.Ordinal))),
            semanticRenames = new[] { new { previousAddress, currentAddress } }
        }).GetProperty("result");
        Assert.False(proposal.GetProperty("isError").GetBoolean(), proposal.GetRawText());
        _proposalId = proposal.GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
    }

    void Because() => _result = Call("visualize-model", new { proposalId = _proposalId }).GetProperty("result");

    JsonElement Structured => _result.GetProperty("structuredContent");

    string Text => _result.GetProperty("content")[0].GetProperty("text").GetString()!;

    [Fact] void should_list_the_added_reaction() => Structured.GetProperty("added").EnumerateArray().Select(item => item.GetString()).ShouldContain("Reaction Projects.Registration.NotifyProject.RegistrationNotifier");
    [Fact] void should_list_the_removed_reaction() => Structured.GetProperty("removed").EnumerateArray().Select(item => item.GetString()).ShouldContain("Reaction Projects.Registration.NotifyProject.ProjectNotifier");
    [Fact] void should_count_the_current_reaction() => Structured.GetProperty("current").GetProperty("reactions").GetInt32().ShouldEqual(1);
    [Fact] void should_count_the_proposed_reaction() => Structured.GetProperty("proposed").GetProperty("reactions").GetInt32().ShouldEqual(1);
    [Fact] void should_describe_the_added_reaction() => Text.Contains("Added: Reaction Projects.Registration.NotifyProject.RegistrationNotifier", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_describe_the_removed_reaction() => Text.Contains("Removed: Reaction Projects.Registration.NotifyProject.ProjectNotifier", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_describe_the_reaction_count() => Text.Contains("1 reaction(s)", StringComparison.Ordinal).ShouldBeTrue();
}
