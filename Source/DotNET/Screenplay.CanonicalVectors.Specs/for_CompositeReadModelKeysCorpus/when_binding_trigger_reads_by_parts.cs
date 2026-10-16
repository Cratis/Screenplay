// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_binding_trigger_reads_by_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("invokes Update\n  first = first\n  second = second", true)]
    [InlineData("produces Recorded\n  first = first\n  second = second", false)]
    [InlineData("file FollowUp.cs", false)]
    public void should_report_only_invoking_reads_as_information(string effect, bool admitted)
    {
        var source = "trigger Requested\n  first String\n  second String\n" + Declarations + "\n    slice Automation Reactions\n" + """
                  command Update
                    first String
                    second String
                    produces Recorded
                      first = first
                      second = second
                  reaction FollowUp
                    runs as system
                    when Requested
                      reads Row
                        by
                          first = first
                          second = second
            """ + "\n" + Indent(effect, 10);
        var result = Bind(source);
        Assert.True(result.Success == admitted, string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var reads = result.Diagnostics.Where(diagnostic => diagnostic.Message.Contains("decision", StringComparison.OrdinalIgnoreCase)).ToArray();
        reads.ShouldContainSingleItem();
        reads.Single().Code.ShouldEqual(admitted ? DiagnosticCodes.ReportOnlySemanticSyntax : DiagnosticCodes.UnsupportedSemanticSyntax);
        reads.Single().Severity.ShouldEqual(admitted ? DiagnosticSeverity.Information : DiagnosticSeverity.Error);
        if (admitted)
        {
            result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
            result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        }
        else
        {
            result.Value.ShouldBeNull();
        }
    }
}
