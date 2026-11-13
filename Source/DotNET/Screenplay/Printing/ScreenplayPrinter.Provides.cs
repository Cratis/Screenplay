// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    void WriteProvides(ScreenplayWriter writer, IEnumerable<ProvideSyntax> provides)
    {
        foreach (var provide in provides) WriteProvide(writer, provide);
    }

    void WriteAvailable(ScreenplayWriter writer, IEnumerable<AvailableSyntax> gates)
    {
        foreach (var gate in gates) writer.Line($"available {ScreenplaySyntaxText.AvailabilityRequirement(gate.Requirement)}", gate);
    }

    void WriteAudience(ScreenplayWriter writer, AudienceSyntax audience)
    {
        using var anchor = writer.Anchor(audience);
        writer.Line($"audience {audience.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, audience.Description, audience);
        }
    }

    void WriteAvailability(ScreenplayWriter writer, AvailabilitySyntax availability)
    {
        using var anchor = writer.Anchor(availability);
        writer.Line($"availability {availability.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, availability.Description, availability);
            foreach (var requirement in availability.Requirements)
            {
                writer.Line(ScreenplaySyntaxText.AvailabilityRequirement(requirement), requirement);
            }
        }
    }

    void WriteProvide(ScreenplayWriter writer, ProvideSyntax provide)
    {
        using var anchor = writer.Anchor(provide);
        var declaration = $"provide {provide.Name} {ScreenplaySyntaxText.TypeRef(provide.Type)}";
        switch (provide.Source)
        {
            case QueryProvideSourceSyntax query:
                var by = query.By.Any() ? $" by {string.Join(", ", query.By.Select(writer.Expression))}" : string.Empty;
                writer.Line($"{declaration} from query {query.Query}{by}", query);
                break;
            case HostProvideSourceSyntax host:
                writer.Line($"{declaration} from host", host);
                break;
            case ValueProvideSourceSyntax value:
                writer.Line($"{declaration} from {writer.Expression(value.Value)}", value);
                break;
            case CodeProvideSourceSyntax code:
                writer.Line(declaration);
                using (writer.Indent())
                {
                    using var sourceAnchor = writer.Anchor(code);
                    WriteCodeBlock(writer, code.Code);
                }

                break;
            case FileProvideSourceSyntax file:
                writer.Line(declaration);
                using (writer.Indent())
                {
                    using var sourceAnchor = writer.Anchor(file);
                    WriteFile(writer, file.File);
                }

                break;
            default:
                throw new UnsupportedSyntaxForPrinting("provide source", provide.Source.GetType().Name);
        }
    }
}
