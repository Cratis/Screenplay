// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        void ValidateReactionIdentities(ImmutableArray<SemanticModule> modules, ImmutableArray<SemanticPolicy> policies)
        {
            var commands = modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Commands)
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single() : null, StringComparer.Ordinal);
            foreach (var reaction in _automationSlices.Values.SelectMany(entry => entry.Slice.Reactions).Where(reaction => reaction.RunsAs is not null))
            {
                var identity = reaction.RunsAs!;
                var roles = identity.Roles.ToHashSet(StringComparer.Ordinal);
                var invocations = reaction.Triggers.SelectMany(trigger => trigger.Invokes ?? []).ToArray();
                var invoked = invocations.Select(invocation => commands.GetValueOrDefault(invocation.Command)).ToArray();
                foreach (var invocation in invocations)
                {
                    if (commands.GetValueOrDefault(invocation.Command) is { } command && ReactionIdentityAnalysis.Allows(command.Authorization, policies, roles) == false)
                    {
                        _diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.UnsatisfiedReactionIdentity, $"Reaction '{reaction.Name}' system identity definitely cannot satisfy command '{invocation.Command}' authorization.", invocation.Location));
                    }
                }

                if (invoked.Length == 0 || invoked.Any(command => command is null || ReactionIdentityAnalysis.Opaque(command.Authorization, policies)) ||
                    reaction.Triggers.Any(trigger => trigger.Code is not null || trigger.File is not null))
                {
                    continue;
                }
                var used = invoked.SelectMany(command => ReactionIdentityAnalysis.Roles(command!.Authorization, policies)).ToHashSet(StringComparer.Ordinal);
                foreach (var role in roles.Where(role => !used.Contains(role)))
                {
                    _diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.UnusedReactionRole, $"Reaction '{reaction.Name}' role '{role}' is referenced by no invoked command's effective authorization gate.", identity.Location));
                }
            }
        }
    }
}
