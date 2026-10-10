// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_read_model_keys : given.a_printer
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      readmodel Row\n        resourceId String key // resource\n        period Int key\n      query Find => Row optional\n        by\n          period Int // month\n          resourceId String\n      command C\n        resourceId String\n        month Int\n        reads Row\n          by // key parts\n            period = month // period source\n            resourceId = resourceId\n";
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();

    [Fact] void should_preserve_the_by_header_comment() => _roundtrip.Printed.ShouldContain("by // key parts");
    [Fact] void should_parse_both_forms() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_key_marks() => Slice.ReadModels!.Single().Properties.All(property => property.IsKey).ShouldBeTrue();
    [Fact] void should_keep_query_part_order() => Slice.Queries.Single().ByParts.Select(part => part.Name).ShouldEqual("period", "resourceId");
    [Fact] void should_keep_reads_part_order() => Slice.Commands.Single().Reads!.Single().ByParts.Select(part => part.Property).ShouldEqual("period", "resourceId");
}
