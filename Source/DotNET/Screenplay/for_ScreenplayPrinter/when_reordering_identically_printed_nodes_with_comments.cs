// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_reordering_identically_printed_nodes_with_comments : given.a_printer
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateChange Place
              command Place
                produces Placed
                  // first tag
                  tag "same" // first tag end
                  // second tag
                  tag "same" // second tag end
                  // first mapping
                  id = id // first mapping end
                  // second mapping
                  id = id // second mapping end
              event Placed
                id Uuid
        """;

    string _printed = string.Empty;

    void Because()
    {
        var application = _compiler.Compile(Source).Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var produces = command.Produces.Single();
        var reordered = produces with
        {
            Tags = [.. produces.Tags.Reverse()],
            Mappings = [.. produces.Mappings.Reverse()]
        };
        _printed = _printer.Print(application with
        {
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with { Commands = [command with { Produces = [reordered] }] }]
                }]
            }]
        });
    }

    [Fact] void should_keep_the_second_tag_comment_on_the_first_tag() => _printed.ShouldContain("// second tag\n          tag same // second tag end");
    [Fact] void should_keep_the_first_tag_comment_on_the_second_tag() => _printed.ShouldContain("// first tag\n          tag same // first tag end");
    [Fact] void should_keep_the_second_mapping_comment_on_the_first_mapping() => _printed.ShouldContain("// second mapping\n          id = id // second mapping end");
    [Fact] void should_keep_the_first_mapping_comment_on_the_second_mapping() => _printed.ShouldContain("// first mapping\n          id = id // first mapping end");
}
