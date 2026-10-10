// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_reaction_identity : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip("module M\n  feature F\n    slice Automation S\n      event E\n      command C\n      reaction R\n        when E\n          invokes C\n        // The trusted returned-command path.\n        runs as system role \"A\\\"B\" and role \"Auditor\"\n        description \"Runs after E\"");

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_round_trip_the_tree() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_print_before_the_trigger() => (_roundtrip.Printed.IndexOf("runs as", StringComparison.Ordinal) < _roundtrip.Printed.IndexOf("when E", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_comment() => _roundtrip.Printed.ShouldContain("// The trusted returned-command path.");
    [Fact] void should_be_canonically_stable() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
