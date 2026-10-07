// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_refusal_and_redelivery_targets : given.an_authoring_connection
{
    const string Source = """
        module Billing
          feature Claims
            slice Automation Handling
              event Approved
              event Claimed
              event Refused
              command Claim
                produces Claimed
              constraint Unique
                unique event Claimed
              reaction Claimer
                when Approved
                  invokes Claim
                    on refused by constraint Unique
                      produces Refused
              specification Recovery
                given Approved
                when redelivered Approved to Claimer
                then Refused
        """;

    [Theory]
    [InlineData("ReactionSyntax", "Claimer")]
    [InlineData("UniqueEventConstraintSyntax", "Unique")]
    public void should_only_propose_identity_preserving_renames_without_changing_disk(string kind, string name)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var response = Call("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Node(kind, revision).GetProperty("handle"),
            expectedName = name,
            newName = $"{name}Again"
        });
        var proposal = response.GetProperty("result").GetProperty("structuredContent");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
        if (kind == "UniqueEventConstraintSyntax")
        {
            proposal.GetProperty("conflicts").EnumerateArray().Single().GetProperty("message").GetString()!.ShouldContain("renaming starts an empty constraint index");
            return;
        }

        var candidate = Candidate(proposal);
        var nodes = WorkspaceSyntaxIndex.Create(candidate).Entries.Select(entry => entry.Node).ToArray();
        nodes.OfType<SpecificationRedeliverySyntax>().Single().Reaction.ShouldEqual(name == "Claimer" ? "ClaimerAgain" : "Claimer");
        nodes.OfType<InvocationRefusalSyntax>().Single().Constraint.ShouldEqual(name == "Unique" ? "UniqueAgain" : "Unique");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }
}
