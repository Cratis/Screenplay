// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_an_application_context : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _duplicate;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var context = compiler.Parse("context\n  businessTimeZone time zone", "application.play");
        _result = PlayFolderMerge.Merge([context, compiler.Parse("concept Id : Uuid", "concept.play")]);
        _duplicate = PlayFolderMerge.Merge([context, compiler.Parse("context", "second.play")]);
    }

    [Fact] void should_merge_a_single_block() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_member() => _result.Value!.Context!.Members.Single().Name.ShouldEqual("businessTimeZone");
    [Fact] void should_reject_a_second_folder_block() => _duplicate.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles);
    [Fact] void should_name_the_repeated_block() => _duplicate.Diagnostics.Single().Message.ShouldContain("context block");
    [Fact] void should_expand_into_the_application_file() => new PlayFileWriter().Expand(_result.Value!).Single(file => file.RelativePath == "application.play").Content.ShouldContain("businessTimeZone time zone");
}
