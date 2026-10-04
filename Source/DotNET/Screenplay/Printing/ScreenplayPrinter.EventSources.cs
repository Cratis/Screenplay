// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    static readonly IReadOnlySet<string> _streamPropertyWords = ReservedWords.CommandBody.Append("stream").ToHashSet(StringComparer.Ordinal);

    internal static void ValidateEventSourceExport(ApplicationSyntax application)
    {
        EventSourceInvariants.Validate(application);
        foreach (var source in application.EventSources)
        {
            if (source is null) throw new InvalidSyntaxJson("Event sources cannot contain null.");

            // The structural transport is the same boundary for metadata-free and programmatic
            // declarations. Validate all fields and children, without binding unknown value types.
            _ = SyntaxJson.Serialize(source);
        }
    }

    void WriteCommandProperties(ScreenplayWriter writer, CommandSyntax command)
    {
        foreach (var property in command.Properties)
        {
            var parts = property.Type.Name.Split('.');

            // A fragment may omit declarations, and syntax JSON deliberately omits source escapes.
            // Preserve the typed property meaning independently of either. Invalid candidates are
            // owned separately and refused before printing; no property can select their meaning.
            var needsEscape = property.Name == "stream" && !property.Type.IsOptional && !property.Type.IsCollection && !property.IsGenerated && !property.IsIdentifier &&
                parts.Length > 1;
            WriteProperties(writer, [property], needsEscape ? _streamPropertyWords : ReservedWords.CommandBody);
        }
    }

    void WriteEventSource(ScreenplayWriter writer, EventSourceSyntax source)
    {
        using var anchor = writer.Anchor(source);
        writer.Line($"eventsource {source.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, source.Description, source);
            if (source.Id is not null) writer.DirectiveLine($"id {StringLiteral.Quote(source.Id)}", source, "id");
            if (source.Identifier is not null) writer.DirectiveLine($"identifier {ScreenplaySyntaxText.TypeRef(source.Identifier)}", source, "identifier");
            foreach (var stream in source.Streams) WriteEventStream(writer, stream);
        }
    }

    void WriteEventStream(ScreenplayWriter writer, EventStreamSyntax stream)
    {
        using var anchor = writer.Anchor(stream);
        writer.Line($"stream {stream.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, stream.Description, stream);
            if (stream.Id is not null) writer.DirectiveLine($"id {StringLiteral.Quote(stream.Id)}", stream, "id");
            if (stream.StreamId is not null) writer.DirectiveLine($"streamId {ScreenplaySyntaxText.TypeRef(stream.StreamId)}", stream, "streamId");
        }
    }

    void WriteCommandStream(ScreenplayWriter writer, CommandStreamSyntax route)
    {
        EventSourceInvariants.Validate(route);
        if (route.PropertyCandidate is not null) throw new InvalidSyntaxJson("An ambiguous stream candidate cannot be exported as .play text.");
        using var anchor = writer.Anchor(route);
        writer.Line($"stream {route.EventSource}.{route.Stream}");
        using (writer.Indent())
        {
            if (route.StreamId is { } mapping) writer.Line($"streamId = {writer.Expression(mapping.Source)}", mapping);
        }
    }
}
