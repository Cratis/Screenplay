// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_is_malformed : given.a_projection_ordering_children
{
    void Because() => _result = _compiler.CompileProjection(Projection("sort by name sideways"));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_ordering() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidChildrenOrdering);
    [Fact] void should_keep_no_ordering() => Children.Ordering.ShouldBeNull();
}
