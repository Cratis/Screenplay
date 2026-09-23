// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_projection_affected_key;

// Chronicle never routes on a projection-level key (ProjectionDefinitionSyntaxVisitor reads only the inline event
// key and the from-block key, ProjectionDefinitionSyntaxVisitor.cs:109-121), so ESM must not either: a projection-level
// key alone binds exactly as no key at all - on the event source identity (ProjectionFactory.cs:1009-1017).
public class with_only_a_projection_level_key : given.a_semantic_binder
{
    const string WithProjectionLevelKey =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                key projectId
                from ProjectRegistered
                  name = name
        """;

    // The blank line stands where the projection-level key was, so both documents report at the same locations.
    const string WithoutAnyKey =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary

                from ProjectRegistered
                  name = name
        """;

    CompilationResult<SemanticCompilation> _withProjectionLevelKey;
    CompilationResult<SemanticCompilation> _withoutAnyKey;

    void Because()
    {
        _withProjectionLevelKey = Bind(WithProjectionLevelKey);
        _withoutAnyKey = Bind(WithoutAnyKey);
    }

    [Fact] void should_bind() => _withProjectionLevelKey.Success.ShouldBeTrue();
    [Fact] void should_key_on_the_event_source_identity() =>
        _withProjectionLevelKey.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(_ => _.Projections).Single().Scope!.From.Single().Key.ShouldEqual(SemanticProjectionKey.EventSourceIdentity);
    [Fact] void should_report_exactly_what_no_key_reports() =>
        _withProjectionLevelKey.Diagnostics.Select(Describe).ShouldContainOnly(_withoutAnyKey.Diagnostics.Select(Describe));

    static string Describe(Diagnostic diagnostic) =>
        $"{diagnostic.Severity}|{diagnostic.Code}|{diagnostic.Message}|{diagnostic.Location.Line}:{diagnostic.Location.Column}";
}
