// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_navigation_regions_of_one_module : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("module Sales\n  navigation sidebar extends\n    item customers title \"Customers\" target Sales.Customers\n  feature Customers\n", "sales.play"),
            compiler.Parse("module Sales\n  navigation topbar overrides\n    item orders title \"Orders\" target Sales.Customers\n", "sales-topbar.play")
        ]);
    }

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_regions_of_every_file() => _result.Value!.Modules.Single().Navigation.Select(region => region.Region).ShouldContainOnly(NavigationRegionKind.Sidebar, NavigationRegionKind.Topbar);
}
