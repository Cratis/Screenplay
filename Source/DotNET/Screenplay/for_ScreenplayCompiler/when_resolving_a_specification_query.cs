// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_resolving_a_specification_query : given.a_compiler
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId Uuid
                name String
              query ProjectById => ProjectSummary?
                by projectId Uuid
            slice StateChange RegisterProject
              specification LookingUpAProject
                then query ProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Screenplay"
        """;

    CompilationResult<ApplicationSyntax> _known;
    CompilationResult<ApplicationSyntax> _unknown;

    void Because()
    {
        _known = _compiler.Compile(Source);
        _unknown = _compiler.Compile(Source.Replace("then query ProjectById", "then query MissingProjectById", StringComparison.Ordinal));
    }

    [Fact] void should_resolve_the_query_from_a_sibling_slice() => _known.Diagnostics.ShouldBeEmpty();
    [Fact] void should_report_an_unknown_query() => _unknown.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownQuery);
}
