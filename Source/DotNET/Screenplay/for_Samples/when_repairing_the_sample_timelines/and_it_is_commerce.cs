// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_Samples.when_repairing_the_sample_timelines;

public class and_it_is_commerce(Xunit.ITestOutputHelper output) : given.a_sample_timeline(output)
{
    [Fact] void should_offer_only_the_planned_repairs() => OffersOnlyThePlannedRepairs("Commerce", 8, 5);
}
