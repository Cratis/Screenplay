// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_removal_key_parts : Specification
{
    ProjectionSyntax _original;
    ProjectionSyntax _roundTripped;
    string _printed;

    void Establish() => _original = new ScreenplayCompiler().CompileProjection(
        """
        projection Totals => Totals
          from PaymentAdded
            key
              invoiceId = invoice
              method = method
          remove with PaymentRemoved
            key
              invoiceId = invoice
              method = method
        """).Value!;

    void Because()
    {
        var restored = (ProjectionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_original));
        _printed = new ScreenplayPrinter().Print(restored);
        var result = new ScreenplayCompiler().CompileProjection(_printed);
        result.Success.ShouldBeTrue();
        _roundTripped = result.Value!;
    }

    [Fact] void should_preserve_every_key_part_through_json_and_source() => SyntaxJson.StructurallyEqual(_original, _roundTripped).ShouldBeTrue();
    [Fact] void should_not_print_trailing_spaces_for_untyped_keys() => _printed.Split('\n').Any(line => line.EndsWith(' ')).ShouldBeFalse();
    [Fact] void should_keep_the_removal_key_untyped() => _roundTripped.Blocks.OfType<RemoveWithSyntax>().Single().PartsKey!.Type.ShouldEqual(string.Empty);
}
