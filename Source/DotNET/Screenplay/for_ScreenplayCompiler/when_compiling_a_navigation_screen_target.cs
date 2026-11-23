// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_screen_target : given.a_compiler
{
    public const string Layout = "layout AppShell\n  category Shell\n  content page routed primary\n  outlet sidebar of navigation\n";
    public const string Source = Layout + """
        module Shop
          outlet sidebar extends
            item projects title "Projects" target Shop.Projects screen Projects
            item create title "Create" screen CreateProject target Shop.Projects
            item group title "More"
              item nested title "Archived" target Shop.Projects.Archived screen Archived
            item adopted title "Shared" target Shop.Projects screen Shared
            item legacy title "Default" target Shop.Projects
          screen Shared
            fits page
          feature Projects
            slice StateView Listing
              screen CreateProject
                fits page
              screen Projects
                fits page
              uses screen Shared
            feature Archived
              slice StateView Listing
                screen Archived
        """;

    CompilationResult<Syntax.ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_selected_screen() => _result.Value.Modules.Single().Outlets.Single().Items.OfType<Syntax.NavigationItemSyntax>().First().Target.Screen.ShouldEqual("Projects");
    [Fact] void should_not_reorder_the_screens() => _result.Value.Modules.Single().Features.First().Slices.Single().Screens.First().Name.ShouldEqual("CreateProject");
    [Fact] void should_leave_the_legacy_screen_unspecified() => _result.Value.Modules.Single().Outlets.Single().Items.OfType<Syntax.NavigationItemSyntax>().Last().Target.Screen.ShouldBeNull();
}
