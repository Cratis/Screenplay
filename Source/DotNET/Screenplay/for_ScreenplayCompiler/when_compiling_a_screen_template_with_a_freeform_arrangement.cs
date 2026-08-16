// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_screen_template_with_a_freeform_arrangement : given.a_compiler
{
    const string Source =
        """
        module Dashboards
          screen template DashboardCanvas
            arrangement freeform
              variant width regular, height regular
                place header  at 0,0    size fill,64
                place sidebar at 0,64   size 240,fill
                place main    at 240,64 size fill,fill

              variant width compact, height regular
                place header at 0,0  size fill,48
                place main   at 0,48 size fill,fill
                place sidebar hidden
        """;

    CompilationResult<ApplicationSyntax> _result;
    ScreenTemplateSyntax _template;
    VariantSyntax _regular;
    VariantSyntax _compact;
    PlaceSyntax _regularSidebar;
    PlaceSyntax _compactSidebar;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _template = _result.Value!.Modules.Single().ScreenTemplates.Single();
        _regular = _template.Arrangement!.Variants!.First();
        _compact = _template.Arrangement!.Variants!.Skip(1).First();
        _regularSidebar = _regular.Places.Single(place => place.SlotName == "sidebar");
        _compactSidebar = _compact.Places.Single(place => place.SlotName == "sidebar");
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_freeform_arrangement() => _template.Arrangement!.Mode.ShouldEqual(ArrangementMode.Freeform);
    [Fact] void should_declare_the_slots_the_variants_place() => _template.Slots.Select(slot => slot.Name).ShouldContainOnly("header", "sidebar", "main");
    [Fact] void should_parse_the_regular_variant_size_class() => (_regular.Width, _regular.Height).ShouldEqual(("regular", "regular"));
    [Fact] void should_parse_the_compact_variant_size_class() => (_compact.Width, _compact.Height).ShouldEqual(("compact", "regular"));
    [Fact] void should_parse_the_regular_sidebar_position() => (_regularSidebar.X, _regularSidebar.Y).ShouldEqual((0, 64));
    [Fact] void should_parse_the_regular_sidebar_size() => (_regularSidebar.SizeWidth, _regularSidebar.SizeHeight).ShouldEqual(("240", "fill"));
    [Fact] void should_parse_the_compact_sidebar_as_hidden() => _compactSidebar.Hidden.ShouldBeTrue();
    [Fact] void should_leave_hidden_sidebar_position_null() => (_compactSidebar.X, _compactSidebar.Y).ShouldEqual((null, null));
}
