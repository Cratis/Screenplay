// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_sorts_descending : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection(Projection("sort by details.addedAt descending"));

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_sort() => Children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Sorted);
    [Fact] void should_sort_by_the_path() => Children.Ordering!.SortBy.ShouldEqual("details.addedAt");
    [Fact] void should_sort_descending() => Children.Ordering!.Direction.ShouldEqual(ChildrenSortDirection.Descending);
}
