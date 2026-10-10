// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_production_routes : Specification
{
    const string Source = "eventsource Account\n  identifier String\n  stream Transactions\n  stream Notes\nmodule Banking\n  feature Posting\n    slice StateChange Record\n      command Record\n        account String identifier\n        stream Account.Transactions\n        produces event Recorded\n          stream Account.Notes";

    [Fact]
    void should_refuse_an_unknown_stream() => new ScreenplayCompiler().Compile(Source.Replace("stream Account.Notes", "stream Account.Missing", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandStream).ShouldBeTrue();

    [Fact]
    void should_warn_about_a_redundant_override() => new ScreenplayCompiler().Compile(Source.Replace("stream Account.Notes", "stream Account.Transactions", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantProductionRoute).ShouldBeTrue();

    [Fact]
    void should_report_an_implicit_inline_override_identifier_mismatch_on_the_override()
    {
        var source = Source.Replace("        stream Account.Transactions\n", string.Empty, StringComparison.Ordinal).Replace("identifier String", "identifier Uuid", StringComparison.Ordinal);
        var result = new ScreenplayCompiler().Compile(source);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandStream).Location.Line.ShouldEqual(11);
    }

    [Fact]
    void should_refuse_an_identifier_type_mismatch() => new ScreenplayCompiler().Compile(Source.Replace("identifier String", "identifier Uuid", StringComparison.Ordinal)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandStream).ShouldBeTrue();
}
