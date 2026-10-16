// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class without_the_preamble : given.a_protected_read_model
{
    [Fact]
    void should_preserve_legacy_refusals_and_report_repair_eligibility_once()
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Lookup", "      command Inspect\n        id String identifier\n        reads View by id\n    slice StateView Lookup", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.PreservedLegacySemanticSyntax).ShouldEqual(2);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("preamble", StringComparison.Ordinal));
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable).ShouldEqual(1);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable).Location.Line.ShouldEqual(10);
    }

    [Fact]
    void should_not_offer_a_repair_for_an_unprotectable_read()
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal).Replace("        remove with Removed\n", string.Empty, StringComparison.Ordinal);
        Bind(source).Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
    }
}
