// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_identity_metadata : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _duplicate;
    PlayFileContent[] _files;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var identity = compiler.Parse("identity\n  department String from claim \"department\"", "application.play");
        var policy = compiler.Parse("policy P\n  require claim \"x\" matches $identity.department", "policy.play");
        _result = PlayFolderMerge.Merge([identity, policy]);
        _duplicate = PlayFolderMerge.Merge([identity, compiler.Parse("identity", "second.play")]);
        _files = [.. new PlayFileWriter().Expand(_result.Value!)];
    }

    [Fact] void should_resolve_details_after_assembly() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_detail() => _result.Value!.Identity!.Details.Single().Name.ShouldEqual("department");
    [Fact] void should_reject_a_second_folder_block() => _duplicate.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles);
    [Fact] void should_expand_metadata_into_the_application_file() => _files.Single(file => file.RelativePath == "application.play").Content.ShouldContain("department String from claim");
}
