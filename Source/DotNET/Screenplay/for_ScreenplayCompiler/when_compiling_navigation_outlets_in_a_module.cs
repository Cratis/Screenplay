// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_outlets_in_a_module : given.a_navigation_document
{
    const string SalesNavigation =
        """
          outlet sidebar extends
            item customers title "Customers" target Sales.Customers
          outlet topbar overrides
            item orders title "Orders" target Sales.Orders
        """;

    const string SupportNavigation =
        """
          outlet sidebar inherits
        """;

    const string Shell =
        """
          outlet sidebar of navigation
          outlet topbar of navigation
        """;

    void Because() => CompileWith(Shell, SalesNavigation, SupportNavigation);

    ModuleSyntax Sales => _result.Value!.Modules.Single(module => module.Name == "Sales");
    ModuleSyntax Support => _result.Value!.Modules.Single(module => module.Name == "Support");

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_extending_sidebar() => Sales.Outlets.Single(outlet => outlet.Name == "sidebar").Mode.ShouldEqual(OutletMode.Extend);
    [Fact] void should_read_the_overriding_topbar() => Sales.Outlets.Single(outlet => outlet.Name == "topbar").Mode.ShouldEqual(OutletMode.Override);
    [Fact] void should_read_the_items_of_a_module_outlet() => Sales.Outlets.First().Items.Single().Id.ShouldEqual("customers");
    [Fact] void should_leave_the_kind_to_the_declaration() => Sales.Outlets.All(outlet => outlet.Kind is null).ShouldBeTrue();
    [Fact] void should_read_an_explicit_inherit() => Support.Outlets.Single().Mode.ShouldEqual(OutletMode.Inherit);
    [Fact] void should_leave_a_module_without_outlets_with_none() => _result.Value!.Modules.Single(module => module.Name == "Empty").Outlets.ShouldBeEmpty();
}
