// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_joins : given.a_protected_read_model
{
    [Theory]
    [InlineData("join related on id\n          with Recorded")]
    [InlineData("remove via join on JoinRemoved")]
    public void should_refuse_joins_including_removal_joins(string block)
    {
        var source = Source.Replace("      event Removed", "      event Removed\n      event JoinRemoved", StringComparison.Ordinal)
            .Replace("        remove with Removed", $"        remove with Removed\n        {block}", StringComparison.Ordinal);
        Refusal(Bind(source), SemanticDecisionReadRefusal.Join);
    }
}
