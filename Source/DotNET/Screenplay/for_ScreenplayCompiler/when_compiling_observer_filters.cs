// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_observer_filters : Specification
{
    const string Prefix = "eventsource Account\n  identifier String\n  stream Transactions\nmodule Banking\n  feature Posting\n    slice StateChange Record\n      event Recorded\n      command Record\n        account String identifier\n        stream Account.Transactions\n        produces event InlineRecorded\n    slice Automation Follow\n      reaction Follow\n";

    [Theory]
    [InlineData("        from Account\n        when Recorded")]
    [InlineData("        from Account.Transactions\n        when Recorded")]
    void should_accept_source_and_stream_filters(string body) =>
        new ScreenplayCompiler().Compile(Prefix + body).Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    [Theory]
    [InlineData("        from Account\n        every 1 day")]
    [InlineData("        from Account\n        from Account\n        when Recorded")]
    [InlineData("        from Account\n          streamId = \"period\"\n        when Recorded")]
    [InlineData("        from Missing\n        when Recorded")]
    void should_refuse_invalid_filters(string body) =>
        new ScreenplayCompiler().Compile(Prefix + body).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidObserverFilter).ShouldBeTrue();

    [Fact]
    void should_warn_when_every_producer_is_excluded()
    {
        var source = Prefix.Replace("        stream Account.Transactions\n", string.Empty, StringComparison.Ordinal).Replace("        produces event InlineRecorded", "        produces Recorded", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(source + "        from Account\n        when Recorded").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ObserverFilterExcludesEveryProducer).ShouldBeTrue();
    }
}
