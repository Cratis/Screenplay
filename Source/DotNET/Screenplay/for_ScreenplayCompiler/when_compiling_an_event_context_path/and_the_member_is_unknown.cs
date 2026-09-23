// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_the_member_is_unknown : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$eventContext.causationId"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_member() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventContextMember);
    [Fact] void should_warn() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_member_and_what_was_expected() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown event context member 'causationId' in '$eventContext.causationId' - expected one of eventType, eventSourceType, eventSourceId, eventStreamType, eventStreamId, sequenceNumber, occurred, eventStore, namespace, correlationId, causation, causedBy, tags, hash, observationState, subject, subjectIsEventSourceId");
}
