// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_declaring_reaction_identity : given.a_compiler
{
    [Theory]
    [InlineData("runs as system")]
    [InlineData("runs as system role \"Automation\"")]
    [InlineData("runs as system role \"Automation\" and role \"Auditor\"")]
    void should_parse_a_single_system_identity(string identity)
    {
        var result = _compiler.Compile(Source(identity));
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single().RunsAs!.Kind.ShouldEqual("system");
    }

    [Theory]
    [InlineData("runs as Persona")]
    [InlineData("runs as system role Name")]
    [InlineData("runs as system role \"\"")]
    [InlineData("runs as system role \"A\" and role \"A\"")]
    [InlineData("runs as system\n        runs as system")]
    [InlineData("runs as system\n          role \"A\"")]
    void should_reject_invalid_identity_declarations(string identity)
    {
        var result = _compiler.Compile(Source(identity));
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReactionIdentity).ShouldEqual(1);
    }

    [Fact]
    void should_reject_identity_under_a_trigger()
    {
        var result = _compiler.Compile("module M\n  feature F\n    slice Automation S\n      event E\n      reaction R\n        when E\n          runs as system");
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidReactionIdentity);
    }

    [Theory]
    [InlineData(false, false, "PLAY0648")]
    [InlineData(false, true, "PLAY0557")]
    [InlineData(true, false, null)]
    [InlineData(true, true, null)]
    void should_report_only_one_missing_identity_warning_per_invocation(bool identity, bool refusal, string? expected)
    {
        var source = "policy Access\n  require authenticated\n" + Source(identity ? "runs as system" : string.Empty)
            .Replace("      command C", "      command C\n        authorize Access", StringComparison.Ordinal)
            + (refusal ? "\n            on refused by authorization\n              acknowledge" : string.Empty);
        var result = _compiler.Compile(source);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(expected is null ? [] : new[] { expected });
    }

    [Fact]
    void should_warn_for_unused_identity()
    {
        var result = _compiler.Compile(Source("runs as system").Replace("\n          invokes C", string.Empty, StringComparison.Ordinal));
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnusedReactionIdentity);
    }

    [Fact]
    void should_not_warn_for_an_implementation_body()
    {
        var result = _compiler.Compile(Source("runs as system").Replace("invokes C", "file R.cs", StringComparison.Ordinal));
        result.Diagnostics.ShouldBeEmpty();
    }

    static string Source(string identity) => $"module M\n  feature F\n    slice Automation S\n      event E\n      command C\n      reaction R\n        {identity}\n        when E\n          invokes C";
}
