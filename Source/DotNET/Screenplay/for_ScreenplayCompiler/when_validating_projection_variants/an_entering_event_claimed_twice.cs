// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_projection_variants;

public class an_entering_event_claimed_twice : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _duplicate;
    CompilationResult<ProjectionSyntax> _distinct;

    void Because()
    {
        _duplicate = _compiler.CompileProjection("projection WorkItem\n  variant BacklogItem\n    enters on IssueCreated\n  variant DevelopmentItem\n    enters on IssueCreated");
        _distinct = _compiler.CompileProjection("projection WorkItem\n  variant BacklogItem\n    enters on IssueCreated\n  variant DevelopmentItem\n    enters on IssueStarted");
    }

    [Fact] void should_report_the_second_claim() => _duplicate.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateVariantEnteringEvent);
    [Fact] void should_accept_distinct_entry_events() => _distinct.Success.ShouldBeTrue();
}
