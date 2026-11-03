// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_failing_trigger_is_named_like_a_built_in : given.documents
{
    const string Watcher = "    slice Automation Watcher\n      reaction Sync\n        when Startup\n          produces Synced\n            for \"watch\"\n            seen = \"yes\"\n      event Synced\n        seen String\n";
    AdmissibleCompilationResult _admissible;

    void Establish() => Source("trigger Startup\n  source\n" + Valid + Watcher);
    void Because() => _admissible = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_reach_binding_and_report_the_untyped_datum() => _normal.Diagnostics.ShouldContain(value => value.Severity == DiagnosticSeverity.Error && value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_preserve_the_strict_failure() => _admissible.Compilation.Success.ShouldBeFalse();
    [Fact] void should_return_the_strict_diagnostics() => _admissible.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
    [Fact] void should_not_exclude_anything() => _admissible.Exclusions.ShouldBeEmpty();
}
