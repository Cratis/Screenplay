// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSyntaxIndex;

public class when_indexing_availability : Specification
{
    McpSyntaxIndex _index;

    void Because()
    {
        var application = new ScreenplayCompiler().Compile("""
            audience Staff
            audience Customers
            availability StaffOnly
              for Staff
            persona Clerk
              audience Staff
            module Shop
              available for Staff or Customers
              feature Counter
                available with StaffOnly
                slice StateChange Refund
                  command Refund
                    produces Refunded
                  event Refunded
            """).Value!;
        _index = new();
        _index.VisitApplication(application);
        _index.Complete(application);
    }

    [Fact] void should_list_each_audience_as_a_declaration() => _index.Declarations.Count(declaration => declaration.Kind == "Audience").ShouldEqual(2);
    [Fact] void should_list_the_availability_as_a_declaration() => _index.Declarations.Count(declaration => declaration.Kind == "Availability").ShouldEqual(1);
    [Fact] void should_resolve_every_audience_a_gate_names() => _index.References.Where(reference => reference.Role == "availableFor").Select(reference => _index.Resolve(reference).Single().Name).Order().ShouldEqual(["Customers", "Staff", "Staff"]);
    [Fact] void should_resolve_the_availability_a_gate_names() => _index.Resolve(_index.References.Single(reference => reference.Role == "availableWith")).Single().Name.ShouldEqual("StaffOnly");
}
