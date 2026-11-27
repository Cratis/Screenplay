// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The parts of the screenplay parser shared by the declarations that sit on a canvas.
/// </summary>
internal static partial class ScreenplayParser
{
    // The state of a canvas declaration's body: its text, where it sits and how big it is.
    class CanvasBodyState
    {
        public string? Description { get; private set; }

        public (int X, int Y)? Position { get; private set; }

        public (int Width, int Height)? Size { get; private set; }

        public Dictionary<string, SourceLocation> DirectiveLocations { get; } = [];

        public void ParseDescription(ParserContext context, SourceLine child, string owner)
        {
            var previous = Description;
            Description = DescriptionParser.Parse(context, child, previous, owner);
            if (previous is null && Description is not null)
            {
                DirectiveLocations["description"] = child.Location;
            }
        }

        public void ParsePosition(ParserContext context, SourceLine child) =>
            Position = ParseCanvasPair(context, child, "position", "x", "y", Position, allowNegative: true) ?? Position;

        public void ParseSize(ParserContext context, SourceLine child) =>
            Size = ParseCanvasPair(context, child, "size", "width", "height", Size, allowNegative: false) ?? Size;
    }
}
