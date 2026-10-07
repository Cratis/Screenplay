// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_resolving_refusal_and_redelivery_references : Specification
{
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Parse("""
            module Billing
              feature Claims
                slice Automation Handling
                  event Approved
                  event Refused
                  constraint Unique
                    unique event Approved
                    unique event Refused
                  reaction Claimer
                    when Approved
                      invokes Claim
                        on refused by constraint Billing.Claims.Handling.Unique
                          produces Refused
                  specification Recovery
                    given Approved
                    when redelivered Approved to Billing.Claims.Handling.Claimer
                    then Refused
            """).Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_treat_a_multi_rule_constraint_as_one_declaration() => _index.Declarations.Count(declaration => declaration.Kind == "Constraint").ShouldEqual(1);
    [Fact] void should_resolve_the_named_constraint() => Target("refusalConstraint").Kind.ShouldEqual("Constraint");
    [Fact] void should_resolve_the_branch_event() => Target("refusalProduces").Kind.ShouldEqual("Event");
    [Fact] void should_resolve_the_redelivered_event() => Target("whenRedeliveredEvent").Name.ShouldEqual("Approved");
    [Fact] void should_resolve_the_redelivery_reaction() => Target("redeliveryReaction").Kind.ShouldEqual("Reaction");
    [Fact] void should_keep_the_branch_reference_owned_by_its_reaction() => _index.References.Single(reference => reference.Role == "refusalProduces").Owner!.Kind.ShouldEqual("Reaction");
    [Fact] void should_keep_the_redelivery_reference_owned_by_its_specification() => _index.References.Single(reference => reference.Role == "redeliveryReaction").Owner!.Kind.ShouldEqual("Specification");

    McpDeclaration Target(string role) => _index.Resolve(_index.References.Single(reference => reference.Role == role)).Single();
}
