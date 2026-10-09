// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_public_event_metadata : given.a_printer
{
    const string Source = """
        // importing a contract, not a file
        import Fulfillment.Dispatched from "../store/\"name\"\\path\nline\tend" // opaque
        module Shipping
          feature Orders
            slice Translate Transfer
              direction inbound // direction comment
              // public contract comment
              public event Shipped generation 1 from "Shipped//origin" // header comment
                id "OldShipped"
                tag "published"
                name String
              public event Shipped generation 2 from "Shipped//origin"
                id "OldShipped"
                name String
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_accept_source() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_accept_printed_source() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_preserve_typed_metadata() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_print_idempotently() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_the_unescaped_origin() => _roundtrip.Reparsed.Value!.Imports.Single().Origin.ShouldEqual("../store/\"name\"\\path\nline\tend");
    [Fact] void should_preserve_generations() => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Events.Select(value => value.Generation).ShouldEqual(1u, 2u);
    [Fact] void should_keep_the_direction_comment_once() => _roundtrip.Printed.Split("// direction comment", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_keep_the_header_comment_once() => _roundtrip.Printed.Split("// header comment", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_keep_the_import_comment_once() => _roundtrip.Printed.Split("// opaque", StringSplitOptions.None).Length.ShouldEqual(2);

    [Theory]
    [InlineData(EventVisibility.Public, null)]
    [InlineData(EventVisibility.Public, "store")]
    void should_refuse_inline_metadata_instead_of_silently_losing_it(EventVisibility visibility, string? origin)
    {
        var syntax = _compiler.Parse("module Shipping\n  feature Orders\n    slice StateChange Ship\n      command Ship\n        produces event Shipped\n").Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var production = command.Produces.Single();
        var changed = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with
        {
            Commands = [command with { Produces = [production with { InlineEvent = production.InlineEvent! with { Visibility = visibility, Origin = origin } }] }]
        }] }] }] };
        Assert.Throws<UnsupportedSyntaxForPrinting>(() => _printer.Print(changed));
    }

    [Fact]
    void should_not_introduce_markers_into_legacy_source()
    {
        const string legacy = "import Fulfillment.Dispatched\nmodule Shipping\n  feature Orders\n    slice Translate Transfer\n      event Shipped\n        name String\n";
        var roundtrip = RoundTrip(legacy);
        roundtrip.Reparsed.Success.ShouldBeTrue();
        roundtrip.Printed.ShouldNotContain("direction");
        roundtrip.Printed.ShouldNotContain("public event");
        roundtrip.Printed.ShouldNotContain("from");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
