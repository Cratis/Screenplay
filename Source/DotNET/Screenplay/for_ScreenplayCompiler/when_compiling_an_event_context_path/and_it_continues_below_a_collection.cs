// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_it_continues_below_a_collection : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$eventContext.causation.occurred"));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_reject_the_path() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.EventContextPathBelowCollection);
    [Fact] void should_say_why() => _result.Diagnostics.Single().Message.ShouldEqual("'$eventContext.causation.occurred' cannot resolve - 'causation' is a collection (IEnumerable<Causation>) and cannot be addressed below");
}
