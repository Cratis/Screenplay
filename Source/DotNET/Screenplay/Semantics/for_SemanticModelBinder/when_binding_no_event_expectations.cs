// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_no_event_expectations : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Work
            slice StateChange Nothing
              command DoNothing
                projectId String identifier
              specification NothingHappens
                when DoNothing
                  projectId = "one"
        """;

    CompilationResult<SemanticCompilation> _explicit;
    CompilationResult<SemanticCompilation> _omitted;
    CompilationResult<SemanticCompilation> _invalid;
    SemanticSpecificationRun _run;

    void Because()
    {
        _omitted = Bind(Source);
        _explicit = Bind(Source + "\n        then no events");
        Assert.True(_omitted.Success, string.Join('\n', _omitted.Diagnostics));
        Assert.True(_explicit.Success, string.Join('\n', _explicit.Diagnostics));
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var invalidSlice = slice with { Specifications = [specification with { ThenNoEvents = true, WhenAppended = new("Happened", [], specification.Location) }] };
        var invalid = syntax with { Modules = [module with { Features = [feature with { Slices = [invalidSlice] }] }] };
        _invalid = _binder.Bind("Projects", invalid, _omitted.Value!.Documents);
        var plan = SemanticExecutionPlan.Compile(_explicit.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_bind_the_explicit_assertion() => _explicit.Success.ShouldBeTrue();
    [Fact] void should_leave_canonical_bytes_unchanged() => SemanticModelCanonicalJson.Serialize(_explicit.Value!.Model).SequenceEqual(SemanticModelCanonicalJson.Serialize(_omitted.Value!.Model)).ShouldBeTrue();
    [Fact] void should_leave_the_semantic_revision_unchanged() => _explicit.Value!.Model.Revision.ShouldEqual(_omitted.Value!.Model.Revision);
    [Fact] void should_leave_the_selected_version_unchanged() => _explicit.Value!.Model.SemanticVersion.ShouldEqual(_omitted.Value!.Model.SemanticVersion);
    [Fact] void should_execute_the_assertion_now() => _run.Passed.ShouldBeTrue();
    [Fact] void should_reject_a_programmatic_append_action() => _invalid.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidNoEventsExpectation).ShouldBeTrue();
}
