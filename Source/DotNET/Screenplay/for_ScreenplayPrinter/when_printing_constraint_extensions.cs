// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_constraint_extensions : given.a_printer
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
                year Int
              event ProjectImported
                code String
                year Int
              event ProjectReleased
              constraint UniqueProject
                unique code, year on ProjectRegistered
                unique code, year on ProjectImported
                released by ProjectReleased
                ignore casing
                message "Duplicate \"project\""
              constraint OneOpening
                unique event ProjectRegistered
                unique event ProjectImported
                released by ProjectReleased
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_idempotently() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_composite_properties() => ((UniquePropertyConstraintSyntax)Constraints[0]).AdditionalProperties.ShouldContain("year");
    [Fact] void should_preserve_multiple_property_events() => Constraints[0].AdditionalRules.Count().ShouldEqual(1);
    [Fact] void should_preserve_multiple_unique_events() => Constraints[1].AdditionalRules.Count().ShouldEqual(1);
    [Fact] void should_preserve_releases() => Constraints[0].ReleasedBy.ShouldContain("ProjectReleased");
    [Fact] void should_preserve_ignore_casing() => Constraints[0].IgnoreCasing.ShouldBeTrue();
    [Fact] void should_preserve_escaped_message() => Constraints[0].Message.ShouldEqual("Duplicate \"project\"");

    ConstraintSyntax[] Constraints => [.. _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Constraints];
}
