// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_invalid_specification_query_results : given.a_compiler
{
    CompilationResult<SpecificationSyntax> _duplicateArguments;
    CompilationResult<SpecificationSyntax> _invalidHeader;
    CompilationResult<SpecificationSyntax> _unknownDirective;

    void Because()
    {
        _invalidHeader = _compiler.CompileSpecification(
            """
            specification LookingUpAProject
              then query
            """);
        _duplicateArguments = _compiler.CompileSpecification(
            """
            specification LookingUpAProject
              then query ProjectById
                arguments
                  projectId = "first"
                arguments
                  projectId = "second"
            """);
        _unknownDirective = _compiler.CompileSpecification(
            """
            specification LookingUpAProject
              then query ProjectById
                returns
                  projectId = "first"
            """);
    }

    [Fact] void should_reject_a_query_without_a_name() => _invalidHeader.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSpecificationQuery);
    [Fact] void should_reject_duplicate_argument_blocks() => _duplicateArguments.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateSpecificationQueryArguments);
    [Fact] void should_reject_an_unknown_query_directive() => _unknownDirective.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownSpecificationQueryDirective);
}
