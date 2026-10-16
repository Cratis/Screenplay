// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_removal_with_key_parts : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(
        """
        projection Totals => Totals
          remove with PaymentRemoved
            key {
              invoiceId = invoice
              method = method
            }
          from PaymentAdded
            key
              invoiceId = invoice
              method = method
            total = amount
        """);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_leave_the_scalar_key_absent() => Removal.Key.ShouldBeNull();
    [Fact] void should_preserve_the_untyped_key() => Removal.PartsKey!.Type.ShouldEqual(string.Empty);
    [Fact] void should_preserve_authored_part_order() => Removal.PartsKey!.Parts.Select(part => part.Property).ShouldEqual(new[] { "invoiceId", "method" });
    [Fact] void should_parse_the_source_expression() => ((PathExpressionSyntax)Removal.PartsKey!.Parts.First().Expression).Path.ShouldEqual("invoice");
    [Fact] void should_keep_the_following_block() => _result.Value!.Blocks.OfType<FromSyntax>().Single().Mappings.Single().Property.ShouldEqual("total");

    RemoveWithSyntax Removal => _result.Value!.Blocks.OfType<RemoveWithSyntax>().Single();
}
