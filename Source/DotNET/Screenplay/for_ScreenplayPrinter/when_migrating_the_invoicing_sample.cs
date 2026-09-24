// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_migrating_the_invoicing_sample : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(for_ScreenplayCompiler.given.Samples.Invoicing);

    [Fact] void should_reparse_without_legacy_fence_warnings() =>
        _roundtrip.Reparsed.Diagnostics.Any(_ => _.Code == DiagnosticCodes.LegacyInlineCodeFence).ShouldBeFalse();

    [Fact] void should_print_every_embedded_language_on_the_fence() =>
        new[] { "csharp", "typescript", "react", "html", "sql" }.All(language => _roundtrip.Printed.Contains($"```{language}", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_print_multiline_descriptions_with_text_info_strings() => _roundtrip.Printed.ShouldContain("```text");
    [Fact] void should_preserve_the_code_validation() => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices
        .Single(_ => _.Name == "RegisterInvoice").Commands.Single().Validations.OfType<Cratis.Screenplay.Syntax.CodeValidateSyntax>().Single().Code.Language.ShouldEqual("csharp");
    [Fact] void should_be_stable_on_the_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
