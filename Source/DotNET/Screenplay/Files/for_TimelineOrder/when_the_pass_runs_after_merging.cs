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
        var compiler = new ScreenplayCompiler();
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = "module M\n  feature F\n    import \"z.play\"\n    import \"a.play\"",
            ["z.play"] = "slice StateView View\n  projection P\n    from E",
            ["a.play"] = "slice StateChange Write\n  event E"
        });
        var (documents, _) = PlayImports.Resolve(["application.play"], source, compiler.Languages);
        _before = PlayFolderMerge.Merge([.. documents.Select(document => compiler.Parse(document.Source, document.Path, document.Placement))]);
        (_, _after) = PlayApplicationAssembly.Compile(compiler, ["application.play"], source);
    }
    [Fact] void should_leave_syntax_json_unchanged() => SyntaxJson.Serialize(_after.Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(_before.Value!).GetRawText());
    [Fact] void should_add_only_the_information_finding() => _after.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0516");
}
