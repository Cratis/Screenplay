// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_legacy_binding_failures : given.a_protected_read_model
{
    [Fact]
    void should_not_offer_a_repair_when_the_probe_reports_invalid_binding_outside_read_locations()
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Lookup", "      command Unrelated\n        id String identifier\n        returns\n          value = missing\n    slice StateView Lookup", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("Response source 'missing'", StringComparison.Ordinal));
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
    }
}
