// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_visuals_and_placements : Specification
{
    const string Source = """
        module M
          feature F
            slice StateChange Add
              event Added
                title String

            slice StateView Review
              shows event Added
              screen Review
                sketch Screens/review.sketch.json
                mockup Screens/review.html
                image Screens/review.png
        """;

    string _printed = null!;
    SliceSyntax _restored = null!;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(Source);
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        _printed = new ScreenplayPrinter().Print(restored);
        _restored = compiler.Parse(_printed).Value!.Modules.Single().Features.Single().Slices.Last();
    }

    [Fact] void should_print_the_placement() => _printed.ShouldContain("      shows event Added\n");
    [Fact] void should_print_the_sketch() => _printed.ShouldContain("        sketch Screens/review.sketch.json\n");
    [Fact] void should_print_the_mockup() => _printed.ShouldContain("        mockup Screens/review.html\n");
    [Fact] void should_print_the_image() => _printed.ShouldContain("        image Screens/review.png");
    [Fact] void should_keep_the_placement_through_json() => _restored.Placements.Single().Name.ShouldEqual("Added");
    [Fact] void should_keep_the_visuals_through_json() => _restored.Screens.Single().Visuals.Count().ShouldEqual(3);
    [Fact] void should_print_stably() => new ScreenplayPrinter().Print(new ScreenplayCompiler().Parse(_printed).Value!).ShouldEqual(_printed);
}
