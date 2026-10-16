// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_command_reads_by_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData(true, true, "PLAY0781")]
    [InlineData(true, false, "PLAY0782")]
    [InlineData(false, true, "PLAY0271")]
    [InlineData(false, false, "PLAY0271")]
    public void should_preserve_the_protected_and_legacy_refusal_order(bool protect, bool identifier, string expected)
    {
        var source = (protect ? "command reads protected\n" : string.Empty) + Declarations + "\n" + """
                  command Update
                    id String
                    first String
                    second String
                    reads Row
                      by
                        first = first
                        second = second
            """;
        if (identifier) source = source.Replace("        id String", "        id String identifier", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        errors.Select(diagnostic => diagnostic.Code).ShouldContainOnly(expected);
        if (protect && identifier) errors.Single().Message.ShouldContain("ByParts");
    }
}
