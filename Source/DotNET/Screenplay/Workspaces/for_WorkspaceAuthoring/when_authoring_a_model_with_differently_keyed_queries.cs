// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_authoring_a_model_with_differently_keyed_queries : Specification
{
    const string Source = """
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
              query ProjectByName => ProjectSummary?
                by name ProjectName
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
        """;

    CompilationResult<ApplicationSyntax> _source;
    WorkspaceAuthoringResult _result;

    void Establish() => _source = new ScreenplayCompiler().Compile(Source);

    void Because()
    {
        var workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Projects"), "Projects");
        _result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new CreateWorkspaceSyntaxDocument("application", PortablePlayPath.Parse("application.play"), _source.Value!)]
        });
    }

    [Fact] void should_be_valid_full_language_source() => _source.Success.ShouldBeTrue();
    [Fact] void should_accept_source_authoring() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_claim_executable_readiness() => _result.ExecutableReady.ShouldBeFalse();
    [Fact] void should_report_the_backend_limitation() => _result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    [Fact] void should_plan_the_new_source_document() => _result.WritePlan!.Entries.Length.ShouldEqual(1);
}
