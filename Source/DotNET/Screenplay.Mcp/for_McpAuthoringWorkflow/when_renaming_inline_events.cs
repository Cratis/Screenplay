// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_inline_events : given.an_authoring_connection
{
    [Theory]
    [InlineData("EventSyntax", "Renamed")]
    [InlineData("SliceSyntax", "Rename")]
    void should_preserve_inline_contract_and_property_identity_in_a_rename_proposal(string kind, string name)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces event Renamed\n          name String = name\n");
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var before = Page("semantics", revision).EnumerateArray().Select(item => item.GetProperty("semanticId").GetString()).Order().ToArray();
        var eventIds = Page("eventContracts", revision).EnumerateArray().Select(item => item.GetProperty("eventContractId").GetString()).ToArray();
        var proposal = Result("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Node(kind, revision).GetProperty("handle"),
            expectedName = name,
            newName = $"{name}Again"
        });
        var candidate = Candidate(proposal);
        var index = WorkspaceSyntaxIndex.Create(candidate);
        var declaration = index.Entries.Single(entry => entry.Node is EventSyntax);
        declaration.SemanticId.ShouldNotBeNull();
        declaration.EventContractId.ShouldNotBeNull();
        index.Entries.Single(entry => entry.Node is PropertySyntax && entry.Parent == declaration.Handle).SemanticId.ShouldNotBeNull();
        candidate.IdentityCatalog.Semantics.Select(assignment => assignment.Id.ToString()).Order().ToArray().ShouldEqual(before);
        candidate.IdentityCatalog.EventContracts.Select(assignment => assignment.Id.ToString()).ToArray().ShouldEqual(eventIds);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldNotContain($"{name}Again");
    }
}
