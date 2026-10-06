// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_the_pass_runs_after_merging : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _before;
    CompilationResult<Syntax.ApplicationSyntax> _after;
    void Because()
    {
        const string source = "module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E";
        var compiler = new ScreenplayCompiler();
        _before = compiler.Parse(source);
        _after = compiler.Compile(source);
    }
    [Fact] void should_leave_syntax_json_unchanged() => SyntaxJson.Serialize(_after.Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(_before.Value!).GetRawText());
    [Fact] void should_add_only_the_information_finding() => _after.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0516");
}
