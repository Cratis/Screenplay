// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    /// <summary>
    /// Prints one node the way the printer writes it inside its parent, without indentation and without comments, so a
    /// caller can put it into existing text - a migration replacing the lines of a retired form, for example.
    /// </summary>
    /// <param name="node">The node to print: an outlet, an outlet item, a slot or a contribution destination.</param>
    /// <returns>The printed lines; a nested line is indented by two spaces per level.</returns>
    /// <exception cref="ArgumentException">The printer does not print the node on its own.</exception>
    internal IReadOnlyList<string> PrintFragment(SyntaxNode node)
    {
        var writer = new ScreenplayWriter();
        switch (node with { SourceComments = [] })
        {
            case OutletSyntax outlet:
                WriteOutlet(writer, outlet);
                break;
            case OutletItemSyntax item:
                WriteOutletItem(writer, item);
                break;
            case SlotSyntax slot:
                WriteSlots(writer, [slot]);
                break;
            case TemplateSlotSyntax slot:
                writer.Line($"slot {slot.Name}", slot);
                break;
            case ComponentSlotSyntax slot:
                writer.Line($"slot {slot.Name}", slot);
                break;
            case ContributionDestinationSyntax destination:
                writer.Line(DestinationLine(destination), destination);
                break;
            default:
                throw new ArgumentException($"The printer does not print a {node.GetType().Name} on its own", nameof(node));
        }

        return writer.ToString().TrimEnd('\n').Split('\n');
    }
}
