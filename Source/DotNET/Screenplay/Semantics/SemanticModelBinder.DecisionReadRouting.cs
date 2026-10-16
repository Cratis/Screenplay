// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // Capture syntax, like its ESM append, has no output route; source filters do not route appends.
        IEnumerable<(SemanticId EventContract, bool Routed)> DecisionReadProducers() =>
            AllSlices().SelectMany(entry => entry.Slice.Commands.SelectMany(command => command.Produces.Select(produced =>
                    (DecisionReadEvent(produced.Event), command.Stream is not null || produced.Stream is not null)))
                .Concat((entry.Slice.Reactions ?? []).SelectMany(reaction => reaction.Triggers).SelectMany(trigger =>
                    (trigger.Produces ?? []).Concat((trigger.Invokes ?? []).SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces)))
                    .Select(produced => (DecisionReadEvent(produced.Event), produced.Stream is not null)))
                .Concat((entry.Slice.Captures ?? []).SelectMany(capture => capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends)))
                    .Select(append => (DecisionReadEvent(append.Event), false))));

        SemanticId DecisionReadEvent(string name) => _events.TryGetValue(name, out var fact) ? fact.Contract.Id : default;
    }
}
