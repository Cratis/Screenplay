// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_compiling_a_folder_with_an_application_root : Specification
{
    DirectoryInfo _root;
    IReadOnlyList<Diagnostic> _diagnostics;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("play413");
        File.WriteAllText(Path.Combine(_root.FullName, "application.play"), "module M\n  feature F\n    import \"z.play\"\n    import \"a.play\"");
        File.WriteAllText(Path.Combine(_root.FullName, "z.play"), "slice StateView View\n  projection P\n    from E");
        File.WriteAllText(Path.Combine(_root.FullName, "a.play"), "slice StateChange Write\n  event E");
    }

    void Because() => _diagnostics = [.. new PlayFileCompiler().CompileFolder(_root.FullName).Result.Diagnostics];

    [Fact] void should_use_authored_order_after_merging() => _diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Path}:{diagnostic.Location.Line}").ShouldContainOnly("PLAY0516@z.play:3");
    void Destroy() => _root.Delete(true);
}
