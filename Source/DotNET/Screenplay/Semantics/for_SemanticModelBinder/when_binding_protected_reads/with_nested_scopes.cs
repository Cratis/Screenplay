// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_nested_scopes : given.a_protected_read_model
{
    [Theory]
    [InlineData("detail Detail optional", "nested detail", "value = value")]
    [InlineData("detail Detail[]", "children detail identified by id", "id = $eventSourceId")]
    public void should_refuse_child_and_nested_projection_scopes(string property, string header, string mapping)
    {
        var source = "type Detail\n  id String\n  value String\n" + Source.Replace("id String key", $"id String key\n        {property}", StringComparison.Ordinal)
            .Replace("        remove with Removed", $"        remove with Removed\n        {header}\n          from Recorded\n            {mapping}", StringComparison.Ordinal);
        Refusal(Bind(source), SemanticDecisionReadRefusal.Children);
    }
}
