// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_reporting_pattern_grammar_diagnostics
{
    [Fact]
    void should_report_each_failure_at_the_authored_generation_declaration()
    {
        foreach (var vector in given.shared_generation_vectors.Read("invalid"))
        {
            var pattern = vector.GetProperty("pattern").GetString()!;
            var checksum = given.shared_generation_vectors.Text(vector, "checksum");
            var source = "concept Reference : String\n  generate pattern " + StringLiteral.Quote(pattern) + (checksum is null ? string.Empty : " checksum " + checksum);
            var result = new ScreenplayCompiler().Compile(source);
            var reason = vector.GetProperty("reason").GetString()!;
            var code = reason.Contains("expanded length", StringComparison.Ordinal) ? DiagnosticCodes.GenerationSizeOutsideRange : DiagnosticCodes.InvalidGenerationArgument;
            var diagnostic = result.Diagnostics.Single(value => value.Code == code);
            diagnostic.Message.ShouldContain(reason, StringComparison.Ordinal);
            diagnostic.Location.Line.ShouldEqual(2);
        }
    }
}
