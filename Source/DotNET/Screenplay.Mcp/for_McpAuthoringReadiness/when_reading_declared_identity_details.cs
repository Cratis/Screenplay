// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_reading_declared_identity_details : Specification
{
    const string Metadata = "identity\n  department String from claim \"department\"\n";
    const string Command = "module M\n  feature F\n    slice StateChange S\n      command Record\n        produces Recorded\n          value = $identity.department\n      event Recorded\n        value String";
    ApplicationSyntax _application;
    McpAuthoringReadiness _readiness;

    void Because()
    {
        _application = new ScreenplayCompiler().Compile(Metadata + Command).Value!;
        _readiness = new(_application);
    }

    [Fact] void should_report_the_model_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeTrue();
    [Fact] void should_explain_the_pending_admission() => _readiness.ModelExecutionReadiness!.ShouldContain("#600");
    [Fact] void should_report_the_command_as_not_ready() => _readiness.SyntaxOnly(_application.Modules.Single().Features.Single().Slices.Single().Commands.Single()).ShouldBeTrue();
    [Fact] void should_report_the_module_as_not_ready() => _readiness.SyntaxOnly(_application.Modules.Single()).ShouldBeTrue();
    [Fact] void should_not_confuse_the_block_with_an_executable_read() => _readiness.SyntaxOnly(_application.Identity!).ShouldBeFalse();

    [Theory]
    [InlineData("module M\n  feature F\n    slice StateView S\n      readmodel View\n      query Q => View\n        by id String from $identity.department")]
    [InlineData("policy P\n  require claim \"department\" matches $identity.department")]
    void should_report_query_and_policy_reads_as_not_ready(string source)
    {
        var application = new ScreenplayCompiler().Compile(Metadata + source).Value!;
        var readiness = new McpAuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("#600");
        var member = application.Policies.Cast<SyntaxNode>().Concat(application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Queries)).Single();
        readiness.ExecutionReadiness(member)!.ShouldContain("#600");
    }
}
