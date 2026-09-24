// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_reordering_identical_declarations_with_comments : given.a_printer
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                // first declaration
                produces Placed // first trailing
                // second declaration
                produces Placed // second trailing
              event Placed
        """;

    string _printed = string.Empty;

    void Because()
    {
        var application = _compiler.Compile(Source).Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        _printed = _printer.Print(application with
        {
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with { Commands = [command with { Produces = [.. command.Produces.Reverse()] }] }]
                }]
            }]
        });
    }

    [Fact] void should_move_the_second_leading_and_trailing_comments_together() => _printed.ShouldContain("// second declaration\n        produces Placed // second trailing");
    [Fact] void should_move_the_first_leading_and_trailing_comments_together() => _printed.ShouldContain("// first declaration\n        produces Placed // first trailing");
    [Fact] void should_print_the_second_member_first() => _printed.IndexOf("// second declaration", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("// first declaration", StringComparison.Ordinal));
}
