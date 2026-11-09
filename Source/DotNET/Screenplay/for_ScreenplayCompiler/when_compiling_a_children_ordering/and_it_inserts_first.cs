// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_inserts_first : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection(Projection("insert first"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_prepend() => Children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
    [Fact] void should_not_sort() => Children.Ordering!.SortBy.ShouldBeNull();
    [Fact] void should_keep_the_handler() => Children.Blocks.OfType<FromSyntax>().Count().ShouldEqual(1);
}
