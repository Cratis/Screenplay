// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_flow_layout : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          layout MasterDetail
            arrangement flow

            template
              row
                sidebar width 280
                main grow

              when width compact
                column
                  main
                  sidebar
        """;

    CompilationResult<ApplicationSyntax> _result;
    LayoutSyntax _layout;
    TemplateContainerSyntax _root;
    TemplateContainerSyntax _row;
    TemplateSlotSyntax _sidebar;
    TemplateSlotSyntax _main;
    TemplateOverrideSyntax _override;
    TemplateContainerSyntax _overrideColumn;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _layout = _result.Value!.Modules.Single().Layouts.Single();
        _root = (TemplateContainerSyntax)_layout.Template!.Root;
        _row = (TemplateContainerSyntax)_root.Children.Single();
        _sidebar = (TemplateSlotSyntax)_row.Children.First();
        _main = (TemplateSlotSyntax)_row.Children.Skip(1).First();
        _override = _layout.Template!.Overrides.Single();
        _overrideColumn = (TemplateContainerSyntax)((TemplateContainerSyntax)_override.Root).Children.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_flow_arrangement() => _layout.Arrangement.ShouldEqual(LayoutArrangement.Flow);
    [Fact] void should_flatten_slots_from_the_base_template() => _layout.Slots.Select(slot => slot.Name).ShouldContainOnly("sidebar", "main");
    [Fact] void should_wrap_the_row_in_the_implicit_root() => _root.Kind.ShouldEqual(TemplateContainerKind.Flat);
    [Fact] void should_parse_the_row_container() => _row.Kind.ShouldEqual(TemplateContainerKind.Row);
    [Fact] void should_parse_the_sidebar_width() => _sidebar.Width.ShouldEqual(280);
    [Fact] void should_parse_the_main_grow() => _main.Grow.ShouldBeTrue();
    [Fact] void should_parse_the_override_width_condition() => _override.Width.ShouldEqual("compact");
    [Fact] void should_parse_the_override_height_condition() => _override.Height.ShouldBeNull();
    [Fact] void should_parse_the_override_column() => _overrideColumn.Kind.ShouldEqual(TemplateContainerKind.Column);
}
