// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Completeness;

static class PrivilegeCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var authorization = new AuthorizationRoleAnalysis(application);
        var producers = new List<(EventSyntax Event, string Name, SourceLocation Location, SemanticAuthorization? Gate)>();
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                var gate = authorization.Commands.First(entry => ReferenceEquals(entry.Command, command)).Gate;
                foreach (var produced in command.Produces.Where(produced => declarations.Productions.IsEventProduction(produced, slice)))
                {
                    if (declarations.Event(produced.Event, scope) is { } @event) producers.Add((@event, $"command '{command.Name}'", command.Location, gate));
                }
            }
            foreach (var capture in slice.Captures)
            {
                var walker = new CaptureAppends();
                walker.VisitCapture(capture);
                foreach (var append in walker.Appends)
                {
                    if (declarations.Event(append.Event, scope) is { } @event) producers.Add((@event, $"capture '{capture.Name}'", capture.Location, null));
                }
            }
            foreach (var reaction in slice.Reactions)
            {
                foreach (var produced in reaction.Triggers.SelectMany(ReactionProductions.In))
                {
                    if (declarations.Event(produced.Event, scope) is { } @event) producers.Add((@event, $"reaction '{reaction.Name}'", reaction.Location, null));
                }
            }
        }

        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var reaction in slice.Reactions.Where(reaction => reaction.RunsAs is not null))
            {
                var roles = reaction.RunsAs!.Roles.ToArray();
                if (roles.Length == 0) continue;
                foreach (var trigger in reaction.Triggers.Select(trigger => trigger.Source).OfType<NamedTriggerSourceSyntax>())
                {
                    // A declared application trigger and a clock occurrence have no event producer.
                    if (declarations.Event(trigger.Name, scope) is not { } @event) continue;
                    foreach (var producer in producers.Where(producer => ReferenceEquals(producer.Event, @event)).Distinct())
                    {
                        if (ReactionIdentityAnalysis.Opaque(producer.Gate, authorization.Policies))
                        {
                            yield return new(
                                DiagnosticSeverity.Information,
                                DiagnosticCodes.ReactionPrivilegeEscalation,
                                $"The effective gate of {producer.Name}, producing '{@event.Name}' for reaction '{reaction.Name}', cannot be compared with its declared system roles.",
                                producer.Location);
                        }
                        else if (roles.Any(role => !ReactionIdentityAnalysis.Requires(producer.Gate, authorization.Policies, role)))
                        {
                            yield return Diagnostic.Warning(
                                DiagnosticCodes.ReactionPrivilegeEscalation,
                                $"Unprivileged {producer.Name} produces '{@event.Name}' for elevated reaction '{reaction.Name}'; its effective gate does not require every declared system role.",
                                producer.Location);
                        }
                    }
                }
            }
        }
    }

    sealed class CaptureAppends : ScreenplaySyntaxWalker
    {
        internal List<CaptureAppendSyntax> Appends { get; } = [];
        public override void VisitCaptureAppend(CaptureAppendSyntax syntax) => Appends.Add(syntax);
    }
}
