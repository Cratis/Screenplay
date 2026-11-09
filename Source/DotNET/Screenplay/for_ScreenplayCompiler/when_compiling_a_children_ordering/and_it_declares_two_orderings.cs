// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_declares_two_orderings : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection(Projection("insert first", "sort by name"));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.DuplicateChildrenOrdering);
    [Fact] void should_keep_the_first() => Children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
}
