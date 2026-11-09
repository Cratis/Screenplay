// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_children_ordering;

public class and_it_sorts_by_an_undeclared_property : for_ScreenplayCompiler.given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        type OrderLine
          lineId Guid
          product String
        module Orders
          feature Tracking
            slice StateView Orders
              event LineAdded
                lineId Guid
                product String
              readmodel OrderView
                lines OrderLine[]
              projection Orders => OrderView
                children lines identified by lineId
                  sort by price descending
                  from LineAdded
        """);

    [Fact] void should_compile_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_sort_property() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnknownChildrenSortProperty);
    [Fact] void should_locate_the_ordering() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownChildrenSortProperty).Location.Line.ShouldEqual(14);
}
