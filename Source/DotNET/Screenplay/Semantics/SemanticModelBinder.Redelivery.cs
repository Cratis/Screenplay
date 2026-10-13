// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticSpecificationRedelivery? BindRedelivery(SemanticAddress slice, SpecificationSyntax specification)
        {
            if (specification.WhenRedelivered is not { } action) return null;
            var declarations = new ConsistencyDeclarations(syntax, [.. ScreenplayValidator.ScopedSlices(syntax)]);
            var scope = new DeclarationScope([.. slice.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key)]);
            var @event = declarations.Event(action.EventType, scope);
            var reaction = declarations.Resolve(action.Reaction, scope, owner => owner.Reactions, node => node.Name);
            if (reaction is not { } resolved || @event is null || !resolved.Node.Triggers.Any(trigger =>
                trigger.Source is Syntax.NamedTriggerSourceSyntax named && ReferenceEquals(declarations.Event(named.Name, resolved.Scope), @event)))
            {
                Error(DiagnosticCodes.UnknownRedeliveryReaction, $"Reaction '{action.Reaction}' must resolve unambiguously and observe event '{action.EventType}'.", action.Location);
                return null;
            }

            var candidates = specification.Given.Select((given, index) => (Given: given, Index: index))
                .Where(item => ReferenceEquals(declarations.Event(item.Given.EventType, scope), @event))
                .Select(item => (item.Index, Matches: SpecificationRedeliveryValidator.Matches(item.Given, action, @event, declarations, syntax))).ToArray();
            var matches = candidates.Where(item => item.Matches == true).ToArray();
            if (candidates.Any(item => item.Matches is null) || matches.Length != 1)
            {
                Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, $"Redelivery of '{action.EventType}' must identify exactly one decidable given occurrence.", action.Location);
                return null;
            }

            var parts = resolved.Scope.Segments;
            var owner = SemanticAddress.ForSlice(_applicationIdentity, parts[0], [.. parts.Skip(1).SkipLast(1)], parts[^1]);
            var address = SemanticAddress.ForReaction(owner, resolved.Node.Name);
            return new(documents.IdentityCatalog.ResolveSemanticAssignment(address).Id, matches[0].Index);
        }
    }
}
