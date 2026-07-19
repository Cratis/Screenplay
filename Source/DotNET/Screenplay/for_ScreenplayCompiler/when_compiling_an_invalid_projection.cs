// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_projection : given.a_compiler
{
    const string Source =
        """
        projection Order => OrderReadModel
          from OrderPlaced
            key orderId
            key orderNumber
            bogus mapping here
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_key() => _result.Diagnostics.First().Message.ShouldEqual("Duplicate key directive - a from block can only declare one key");
    [Fact] void should_report_the_invalid_mapping() => _result.Diagnostics.Last().Message.ShouldEqual("Invalid mapping 'bogus mapping here'");
    [Fact] void should_carry_locations() => _result.Diagnostics.All(_ => _.Location.Line > 0).ShouldBeTrue();
}
