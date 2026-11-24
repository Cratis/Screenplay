// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_indexing_gates_on_screen_elements : Specification
{
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Compile("""
            audience Staff
            audience Customers
            availability StaffOnly
              for Staff
            module Shop
              feature Counter
                slice StateChange Refund
                  event Refunded
                    reason String
                  command Refund
                    reason String
                    produces Refunded
                      reason = reason
                  screen RefundScreen
                    action Refund
                      available for Staff and not Customers
                      available with StaffOnly
            """).Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_resolve_the_audiences_the_gate_of_an_action_and_the_availability_name() => _index.References.Where(reference => reference.Role == "availableFor").Select(reference => _index.Resolve(reference).Single().Name).Order().ShouldEqual(["Customers", "Staff", "Staff"]);
    [Fact] void should_resolve_the_availability_the_gate_of_an_action_names() => _index.Resolve(_index.References.Single(reference => reference.Role == "availableWith")).Single().Name.ShouldEqual("StaffOnly");
}
