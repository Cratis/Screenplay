// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_refusal_and_redelivery_references
{
    const string Definitions = """
        module Billing
          feature Claims
            slice StateChange Request
              event Approved
              event Claimed
              command Claim
                produces Claimed
              constraint Unique
                unique event Claimed
        """;
    const string Handling = """
        module Billing
          feature Claims
            slice Automation Handling
              event Refused
              reaction Claimer
                when Approved
                  invokes Claim
                    on refused by constraint Billing.Claims.Request.Unique
                      produces Billing.Claims.Handling.Refused
              specification Recovery
                given Approved
                when redelivered Approved to Billing.Claims.Handling.Claimer
                then Refused
        """;

    [Theory]
    [InlineData("EventSyntax", "Approved")]
    [InlineData("EventSyntax", "Refused")]
    [InlineData("ReactionSyntax", "Claimer")]
    public void should_update_the_new_typed_references_without_admitting_execution(string kind, string name)
    {
        var workspace = Create();
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => entry.Kind == kind && WorkspaceReferenceBindings.Name(entry.Node) == name);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = $"{name}Again"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var after = WorkspaceSyntaxIndex.Create(result.Workspace!);
        var reaction = after.Entries.Select(entry => entry.Node).OfType<ReactionSyntax>().Single();
        var redelivery = after.Entries.Select(entry => entry.Node).OfType<SpecificationRedeliverySyntax>().Single();
        var branch = reaction.Triggers.Single().Invokes!.Single().OnRefused.Single();
        redelivery.EventType.ShouldEqual(name == "Approved" ? "ApprovedAgain" : "Approved");
        redelivery.Reaction.ShouldEqual(name == "Claimer" ? "Billing.Claims.Handling.ClaimerAgain" : "Billing.Claims.Handling.Claimer");
        branch.Constraint.ShouldEqual(name == "Unique" ? "Billing.Claims.Request.UniqueAgain" : "Billing.Claims.Request.Unique");
        branch.Produces.Single().Event.ShouldEqual(name == "Refused" ? "Billing.Claims.Handling.RefusedAgain" : "Billing.Claims.Handling.Refused");
        new WorkspaceReferenceBindings(after).Bindings.Where(binding => binding.Reference.Domain is WorkspaceReferenceDomain.Reaction or WorkspaceReferenceDomain.Constraint).All(binding => binding.Outcome == "resolved").ShouldBeTrue();
        workspace.Documents.Select(document => document.Text).ShouldContain(Handling);
    }

    [Theory]
    [InlineData("ReactionSyntax", "Claimer", "Other")]
    [InlineData("UniqueEventConstraintSyntax", "Unique", "Other")]
    public void should_reject_capture_by_a_same_scope_declaration(string kind, string name, string replacement)
    {
        var other = kind == "ReactionSyntax" ? "\n      reaction Other\n        when Approved" : "\n      constraint Other\n        unique event Claimed";
        var workspace = Create(kind == "ReactionSyntax" ? Handling + other : Handling, kind == "ReactionSyntax" ? Definitions : Definitions + other);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => entry.Kind == kind && WorkspaceReferenceBindings.Name(entry.Node) == name);
        workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = replacement
        }).Accepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n        unique event Approved")]
    public void should_refuse_contract_changing_constraint_renames(string additionalRule)
    {
        var workspace = Create(definitions: Definitions + additionalRule);
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ConstraintSyntax && entry.Member == "constraints");
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Unique",
            NewName = "UniqueAgain"
        });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldContain("renaming starts an empty constraint index");
        workspace.Documents.Select(document => document.Text).ShouldContain(Definitions + additionalRule);
    }

    [Theory]
    [InlineData("EventSyntax")]
    [InlineData("ReactionSyntax")]
    public void should_patch_the_typed_redelivery_token_when_event_and_reaction_share_a_name(string kind)
    {
        var workspace = Create(handling: Handling.Replace("Claimer", "Approved", StringComparison.Ordinal));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Kind == kind && WorkspaceReferenceBindings.Name(entry.Node) == "Approved");
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Approved",
            NewName = "Again"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var action = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationRedeliverySyntax>().Single();
        action.EventType.ShouldEqual(kind == "EventSyntax" ? "Again" : "Approved");
        action.Reaction.ShouldEqual(kind == "ReactionSyntax" ? "Billing.Claims.Handling.Again" : "Billing.Claims.Handling.Approved");
    }

    static ScreenplayWorkspace Create(string handling = Handling, string definitions = Definitions) => ScreenplayWorkspace.Create("Billing",
        [WorkspaceDocument.Create("definitions", PortablePlayPath.Parse("definitions.play"), Encoding.UTF8.GetBytes(definitions)),
         WorkspaceDocument.Create("handling", PortablePlayPath.Parse("handling.play"), Encoding.UTF8.GetBytes(handling))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
}
