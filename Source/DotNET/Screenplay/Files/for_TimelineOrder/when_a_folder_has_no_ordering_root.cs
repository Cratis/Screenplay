// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_a_folder_has_no_ordering_root : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => (_, _result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["a.play", "z.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
    {
        ["a.play"] = "module M\n  feature F\n    slice StateView View\n      projection P\n        from E",
        ["z.play"] = "module M\n  feature F\n    slice StateChange Write\n      event E"
    }));
    [Fact] void should_not_invent_an_authored_timeline() => _result.Diagnostics.ShouldBeEmpty();
}
