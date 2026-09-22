// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_consistency_errors_across_files : given.a_consistent_model
{
    PlayFileCompiler _folderCompiler;
    ApplicationCompilation<ApplicationSyntax> _result;

    void Establish()
    {
        var source = Source.Replace("note not empty", "absent not empty", StringComparison.Ordinal)
            .Replace("startReadingId ObservationId", "startReadingId ScopeId", StringComparison.Ordinal)
            .Replace("type Row\n", "type Row\n  owner String\n", StringComparison.Ordinal)
            .Replace("note = note", "note = \"different\"\n          absent = note", StringComparison.Ordinal)
            .Replace("kind = added", "kind = created", StringComparison.Ordinal);
        var boundary = source.IndexOf("module Recording", StringComparison.Ordinal);
        var types = new PlayFile("/model/types.play", "types.play");
        var slices = new PlayFile("/model/slices.play", "slices.play");
        var files = Substitute.For<IPlayFiles>();
        files.FindIn("model").Returns([types, slices]);
        files.ReadContent(types).Returns(source[..boundary]);
        files.ReadContent(slices).Returns(source[boundary..]);
        _folderCompiler = new(files, _compiler);
    }

    void Because() => _result = _folderCompiler.CompileFolder("model");

    [Fact] void should_apply_all_six_rules_after_merging_the_declarations() => _result.Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownValidationTarget, DiagnosticCodes.IncompatibleReadsKey, DiagnosticCodes.UnpopulatedProjectionField, DiagnosticCodes.UnreachableSpecificationOutcome, DiagnosticCodes.UnknownSpecificationEnumMember, DiagnosticCodes.UnknownEventField);
    [Fact] void should_fail_compilation() => _result.Result.Success.ShouldBeFalse();
    [Fact] void should_report_errors_at_the_offending_file() => _result.Result.Diagnostics.All(diagnostic => diagnostic.Location.Path == "slices.play" && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
}
