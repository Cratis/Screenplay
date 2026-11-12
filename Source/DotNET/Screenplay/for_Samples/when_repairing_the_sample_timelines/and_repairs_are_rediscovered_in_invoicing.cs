// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_Samples.when_repairing_the_sample_timelines;

public class and_repairs_are_rediscovered_in_invoicing(Xunit.ITestOutputHelper output) : given.a_sample_timeline(output)
{
    [Fact] void should_rediscover_the_repairs_step_by_step() => RediscoversTheRepairsStepByStep("Invoicing", 4);
}
