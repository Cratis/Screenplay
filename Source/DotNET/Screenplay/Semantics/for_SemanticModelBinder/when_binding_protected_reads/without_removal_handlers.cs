// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class without_removal_handlers : given.a_protected_read_model
{
    [Fact] void should_refuse_a_view_without_removal_handlers() => Refusal(Bind(Source.Replace("        remove with Removed\n", string.Empty, StringComparison.Ordinal)), SemanticDecisionReadRefusal.NoRemoval);
}
