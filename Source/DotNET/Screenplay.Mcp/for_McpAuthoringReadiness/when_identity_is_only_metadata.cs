// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_identity_is_only_metadata : Specification
{
    const string Source = "identity\n  department String from claim \"department\"\n  organization Organization optional from query Mine by $identity.id\nmodule Organizations\n  feature Membership\n    slice StateView S\n      readmodel Organization\n        name String\n      query Mine => Organization optional\n        by id String";
    McpAuthoringReadiness _readiness;

    void Because() => _readiness = new(new ScreenplayCompiler().Compile(Source).Value!);

    [Fact] void should_not_make_the_model_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeFalse();
    [Fact] void should_have_no_pending_executable_admission() => _readiness.ModelExecutionReadiness.ShouldBeNull();

    [Fact]
    void should_keep_a_model_with_just_the_block_ready()
    {
        var syntax = new ScreenplayCompiler().Compile("identity\n  department String from claim \"department\"").Value!;
        var readiness = new McpAuthoringReadiness(syntax);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
    }

    [Fact]
    void should_keep_builtin_reads_ready()
    {
        var syntax = new ScreenplayCompiler().Compile("identity\n  department String from claim \"department\"\nmodule M\n  feature F\n    slice StateChange S\n      command Record\n        produces Recorded\n          value = $identity.name\n      event Recorded\n        value String").Value!;
        new McpAuthoringReadiness(syntax).ModelSyntaxOnly.ShouldBeFalse();
    }
}
