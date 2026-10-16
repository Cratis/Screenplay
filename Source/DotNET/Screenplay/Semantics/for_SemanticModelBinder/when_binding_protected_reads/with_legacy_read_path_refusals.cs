// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_legacy_read_path_refusals : given.a_protected_read_model
{
    [Theory]
    [InlineData("depth")]
    [InlineData("destination")]
    [InlineData("when")]
    [InlineData("unknown")]
    void should_not_offer_a_repair_leaving_read_paths_refused(string position)
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal);
        source = position switch
        {
            "depth" => source.Replace("View.value", "View.nested.value", StringComparison.Ordinal),
            "destination" => source.Replace("for id", "for View.id", StringComparison.Ordinal),
            "unknown" => source.Replace("View.value", "unknown.value", StringComparison.Ordinal),
            _ => source.Replace("        produces Recorded\n          for id\n          value = View.value", "        produces when View.value != \"\"\n          Recorded\n            for id\n            value = value", StringComparison.Ordinal)
        };
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
    }

    [Fact]
    void should_include_read_paths_in_another_command_without_read_declarations()
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Lookup", "      command Unrelated\n        id String identifier\n        validate\n          require View.value != \"\"\n    slice StateView Lookup", StringComparison.Ordinal);
        Bind(source).Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable);
    }

    [Fact]
    void should_not_treat_an_unrelated_condition_failure_as_a_read_refusal()
    {
        var source = Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal)
            .Replace("    slice StateView Lookup", "      command Unrelated\n        id String identifier\n        validate\n          require missing == \"x\"\n    slice StateView Lookup", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable).ShouldEqual(1);
    }
}
