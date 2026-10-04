// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

internal static partial class EventSourceInvariants
{
    internal static void Validate(SyntaxNode node)
    {
        switch (node)
        {
            case ApplicationSyntax { EventSources: null }:
                throw new InvalidSyntaxJson("Event sources must be a collection.");
            case CommandSyntax command:
                if (command.Stream?.PropertyCandidate is not null) throw new InvalidSyntaxJson("The authoritative command stream cannot contain an ambiguous property candidate.");
                if (command.StreamCandidates is null) throw new InvalidSyntaxJson("Command stream candidates must be a collection.");
                foreach (var rejected in command.StreamCandidates)
                {
                    if (rejected is null) throw new InvalidSyntaxJson("Command stream candidates cannot contain null.");
                    Validate(rejected);
                    if (rejected.PropertyCandidate is null && command.Stream is null) throw new InvalidSyntaxJson("A duplicate route candidate requires an authoritative route.");
                    if (rejected.PropertyCandidate is { } property && command.Properties.Any(member => ReferenceEquals(member, property))) throw new InvalidSyntaxJson("An ambiguous property is owned only by its stream candidate.");
                }
                if (command.Stream is not null) Validate(command.Stream);
                break;
            case EventSourceSyntax source:
                Name(source.Name);
                Pin(source.Id);
                Scalar(source.Identifier);
                if (source.Streams is null) throw new InvalidSyntaxJson("Event streams must be a collection.");
                foreach (var child in source.Streams)
                {
                    if (child is null) throw new InvalidSyntaxJson("Event streams cannot contain null.");
                    Validate(child);
                }
                break;
            case EventStreamSyntax stream:
                Name(stream.Name);
                Pin(stream.Id);
                Scalar(stream.StreamId);
                break;
            case CommandStreamSyntax route:
                Name(route.EventSource);
                Name(route.Stream);
                if (route.StreamId is { Property: not "streamId" }) throw new InvalidSyntaxJson("A command stream maps only streamId.");
                if (route.PropertyCandidate is { } candidate && (candidate.Name != "stream" || candidate.Type.Name != $"{route.EventSource}.{route.Stream}" || candidate.Type.IsCollection || candidate.Type.IsOptional || candidate.IsGenerated || candidate.IsIdentifier || route.StreamId is not null))
                {
                    throw new InvalidSyntaxJson("An ambiguous route must retain its exact unmodified property candidate, without selecting nested routing.");
                }
                break;
        }
    }

    static void Name(string name)
    {
        if (name is null || !NameRegex().IsMatch(name)) throw new InvalidSyntaxJson("Event source and stream names must be identifiers.");
    }

    static void Pin(string? pin)
    {
        if (pin is not null && string.IsNullOrWhiteSpace(pin)) throw new InvalidSyntaxJson("A rename pin must be nonempty.");
    }

    static void Scalar(TypeRefSyntax? type)
    {
        if (type is { IsOptional: true } or { IsCollection: true }) throw new InvalidSyntaxJson("Source identifiers and stream ids require nonoptional scalar type references.");
        if (type is not null && (type.Name is null || !TypeNameRegex().IsMatch(type.Name))) throw new InvalidSyntaxJson("Source identifiers and stream ids require an exact type reference name.");
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*(?![\s\S])", RegexOptions.None, 1000)]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^[\w.]+(?![\s\S])", RegexOptions.None, 1000)]
    private static partial Regex TypeNameRegex();
}
