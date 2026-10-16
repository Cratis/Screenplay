// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_open_subscriptions : given.a_protected_read_model
{
    [Theory]
    [InlineData("every")]
    [InlineData("all")]
    public void should_refuse_an_open_subscription(string block) => Refusal(Bind(Source.Replace("        remove with Removed", $"        remove with Removed\n        {block}\n          value = \"changed\"", StringComparison.Ordinal)), SemanticDecisionReadRefusal.OpenSubscription);
}
