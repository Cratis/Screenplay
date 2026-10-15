// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_regions_in_a_module : given.a_navigation_document
{
    const string SalesNavigation =
        """
          navigation sidebar extends
            item customers title "Customers" target Sales.Customers
          navigation topbar overrides
            item orders title "Orders" target Sales.Orders
        """;

    const string SupportNavigation =
        """
          navigation sidebar inherits
        """;

    void Because() => CompileWith("", SalesNavigation, SupportNavigation);

    ModuleSyntax Sales => _result.Value!.Modules.Single(module => module.Name == "Sales");
    ModuleSyntax Support => _result.Value!.Modules.Single(module => module.Name == "Support");

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_extending_sidebar() => Sales.Navigation.Single(region => region.Region == NavigationRegionKind.Sidebar).Mode.ShouldEqual(NavigationRegionMode.Extend);
    [Fact] void should_read_the_overriding_topbar() => Sales.Navigation.Single(region => region.Region == NavigationRegionKind.Topbar).Mode.ShouldEqual(NavigationRegionMode.Override);
    [Fact] void should_read_the_items_of_a_module_region() => Sales.Navigation.First().Items.Single().Id.ShouldEqual("customers");
    [Fact] void should_read_an_explicit_inherit() => Support.Navigation.Single().Mode.ShouldEqual(NavigationRegionMode.Inherit);
    [Fact] void should_leave_a_module_without_blocks_with_no_regions() => _result.Value!.Modules.Single(module => module.Name == "Empty").Navigation.ShouldBeEmpty();
}
