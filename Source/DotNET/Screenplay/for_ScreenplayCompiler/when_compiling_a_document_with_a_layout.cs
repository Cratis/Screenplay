// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_layout : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          topbar
          navigation contributes Navigation
          content
          footer

          arrangement flow
            column
              topbar height 56
              row
                navigation width 240
                content grow
              footer height 32

        ui profile Desktop
          target platform web
          layout AppShell
        """;

    CompilationResult<ApplicationSyntax> _result;
    LayoutSyntax _layout;
    ArrangementContainerSyntax _column;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _layout = _result.Value!.Layouts!.Single();
        _column = (ArrangementContainerSyntax)((ArrangementContainerSyntax)_layout.Arrangement!.Root!).Children.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_layout_name() => _layout.Name.ShouldEqual("AppShell");
    [Fact] void should_parse_the_slots_in_declaration_order() => _layout.Slots.Select(slot => slot.Name).ShouldContainOnly("topbar", "navigation", "content", "footer");
    [Fact] void should_parse_the_contribution_point_on_the_navigation_slot() => _layout.Slots.Single(slot => slot.Name == "navigation").Contributes.ShouldEqual("Navigation");
    [Fact] void should_leave_the_other_slots_without_a_contribution_point() => _layout.Slots.Where(slot => slot.Name != "navigation").All(slot => slot.Contributes is null).ShouldBeTrue();
    [Fact] void should_arrange_by_flow() => _layout.Arrangement!.Mode.ShouldEqual(ArrangementMode.Flow);
    [Fact] void should_nest_the_tree_directly_under_the_arrangement() => _column.Kind.ShouldEqual(ArrangementContainerKind.Column);
    [Fact] void should_parse_the_topbar_height() => ((ArrangementSlotSyntax)_column.Children.First()).Height.ShouldEqual(56);
    [Fact] void should_have_the_profile_select_the_layout() => _result.Value!.UiProfiles!.Single().Layout.ShouldEqual("AppShell");
}
