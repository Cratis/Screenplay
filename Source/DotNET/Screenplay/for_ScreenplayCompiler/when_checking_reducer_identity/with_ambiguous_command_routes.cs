// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_ambiguous_command_routes : given.a_reducer_identity_model
{
    const string Sources = "import A.S\nimport B.S\ntype S\n  value String\neventsource A\n  stream S\n  stream Main\neventsource B\n  stream S\n";

    [Fact]
    void should_not_filter_an_ambiguous_matching_producer_out_of_the_identity_evidence()
    {
        var source = Sources + Model.Replace("on Recorded", "from A.Main\n        on Recorded", StringComparison.Ordinal)
            .Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)
            .Replace("produces Recorded", "stream A.S\n        stream B.S\n        produces Recorded", StringComparison.Ordinal)
            + SecondCommand.Replace("produces Recorded", "stream A.Main\n        produces Recorded", StringComparison.Ordinal);
        var application = new ScreenplayCompiler().Compile(source).Value!;
        var command = application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Record").Commands.Single();
        command.StreamCandidates.Count().ShouldEqual(2);
        command.Stream.ShouldBeNull();
        Warnings(source).ShouldBeEmpty();
    }
}
