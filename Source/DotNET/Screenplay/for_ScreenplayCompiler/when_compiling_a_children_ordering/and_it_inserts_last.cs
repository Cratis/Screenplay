// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_inserts_last : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection(Projection("insert last"));

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_append() => Children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Append);
}
