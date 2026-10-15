// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_own_capture_reactions;

public class with_an_assembled_folder_model : Specification
{
    CompilationResult<ApplicationSyntax>[] _documents;
    CompilationResult<ApplicationSyntax> _result;

    void Establish()
    {
        var compiler = new ScreenplayCompiler();
        _documents = [
            compiler.Parse("module M\n  feature F\n    slice StateChange Facts\n      event Recorded\n", "facts.play"),
            compiler.Parse("import M.F.Facts.Recorded\nmodule M\n  feature F\n    slice Translate Import\n      capture Feed\n        append Recorded\n      reaction FollowUp\n        when Recorded\n", "import.play")
        ];
    }

    void Because() => _result = PlayFolderMerge.Merge(_documents);

    [Fact] void should_compile_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_after_resolving_the_imported_declaration() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.DirectionlessTranslationReactsToOwnCapture, DiagnosticCodes.RedundantImport);
    [Fact] void should_preserve_the_reaction_file() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DirectionlessTranslationReactsToOwnCapture).Location.Path.ShouldEqual("import.play");
}
