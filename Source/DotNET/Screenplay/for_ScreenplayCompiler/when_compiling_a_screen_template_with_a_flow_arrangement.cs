// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_screen_template_with_a_flow_arrangement : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          screen template MasterDetail
            fits slot content

            sidebar contributes Navigation
            main

            arrangement flow
              row
                sidebar width 280
                main grow

              when width compact
                column
                  main
                  sidebar
        """;

    CompilationResult<ApplicationSyntax> _result;
    ScreenTemplateSyntax _template;
    ArrangementContainerSyntax _root;
    ArrangementContainerSyntax _row;
    ArrangementSlotSyntax _sidebar;
    ArrangementSlotSyntax _main;
    ArrangementOverrideSyntax _override;
    ArrangementContainerSyntax _overrideColumn;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _template = _result.Value!.Modules.Single().ScreenTemplates.Single();
        _root = (ArrangementContainerSyntax)_template.Arrangement!.Root!;
        _row = (ArrangementContainerSyntax)_root.Children.Single();
        _sidebar = (ArrangementSlotSyntax)_row.Children.First();
        _main = (ArrangementSlotSyntax)_row.Children.Skip(1).First();
        _override = _template.Arrangement!.Overrides!.Single();
        _overrideColumn = (ArrangementContainerSyntax)((ArrangementContainerSyntax)_override.Root).Children.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_flow_arrangement() => _template.Arrangement!.Mode.ShouldEqual(ArrangementMode.Flow);
    [Fact] void should_parse_the_slot_it_fits_into() => _template.FitsSlot.ShouldEqual("content");
    [Fact] void should_keep_the_declared_slots() => _template.Slots.Select(slot => slot.Name).ShouldContainOnly("sidebar", "main");
    [Fact] void should_keep_the_contribution_point_on_the_declared_slot() => _template.Slots.First().Contributes.ShouldEqual("Navigation");
    [Fact] void should_wrap_the_row_in_the_implicit_root() => _root.Kind.ShouldEqual(ArrangementContainerKind.Flat);
    [Fact] void should_parse_the_row_container() => _row.Kind.ShouldEqual(ArrangementContainerKind.Row);
    [Fact] void should_parse_the_sidebar_width() => _sidebar.Width.ShouldEqual(280);
    [Fact] void should_parse_the_main_grow() => _main.Grow.ShouldBeTrue();
    [Fact] void should_parse_the_override_width_condition() => _override.Width.ShouldEqual("compact");
    [Fact] void should_parse_the_override_height_condition() => _override.Height.ShouldBeNull();
    [Fact] void should_parse_the_override_column() => _overrideColumn.Kind.ShouldEqual(ArrangementContainerKind.Column);
}
