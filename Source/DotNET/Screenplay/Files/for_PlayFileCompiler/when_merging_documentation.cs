// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_merging_documentation : Specification
{
    const string First = "module M\n  documentation\n    ```markdown\n    Module first.\n    ```\n  feature F\n    documentation\n      ```markdown\n      Feature first.\n      ```\n";
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse(First, "a.play"),
            compiler.Parse(First, "b.play"),
            compiler.Parse(First.Replace("first", "other", StringComparison.Ordinal), "c.play")]);
    }

    [Fact] void should_keep_the_first_module_documentation() => _result.Value!.Modules.Single().Documentation.ShouldEqual("Module first.");
    [Fact] void should_keep_the_first_feature_documentation() => _result.Value!.Modules.Single().Features.Single().Documentation.ShouldEqual("Feature first.");
    [Fact] void should_warn_only_for_the_two_conflicts() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.ConflictingDocumentationAcrossFiles, DiagnosticCodes.ConflictingDocumentationAcrossFiles);
    [Fact] void should_point_to_the_disagreeing_file() => _result.Diagnostics.All(diagnostic => diagnostic.Location.Path == "c.play").ShouldBeTrue();
}
