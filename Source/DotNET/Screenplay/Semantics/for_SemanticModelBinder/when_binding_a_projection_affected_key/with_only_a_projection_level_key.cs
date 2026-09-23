// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_projection_affected_key;

// Chronicle never routes on a projection-level key (ProjectionDefinitionSyntaxVisitor reads only the inline event
// key and the from-block key), so ESM must not either: a projection-level key alone binds exactly as no key at all.
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

    [Fact] void should_not_bind() => _withProjectionLevelKey.Success.ShouldBeFalse();
    [Fact] void should_report_the_unresolved_affected_key() =>
        _withProjectionLevelKey.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidSemanticBinding && _.Message.Contains("requires one resolved affected key")).ShouldBeTrue();
    [Fact] void should_report_exactly_what_no_key_reports() =>
        _withProjectionLevelKey.Diagnostics.Select(Describe).ShouldContainOnly(_withoutAnyKey.Diagnostics.Select(Describe));

    static string Describe(Diagnostic diagnostic) =>
        $"{diagnostic.Severity}|{diagnostic.Code}|{diagnostic.Message}|{diagnostic.Location.Line}:{diagnostic.Location.Column}";
}
