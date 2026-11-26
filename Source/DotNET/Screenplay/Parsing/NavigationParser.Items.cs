// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class NavigationParser
{
    readonly record struct ItemPart(string Key, string Value, bool IsQuoted, bool IsLocalized);

    sealed class ItemParts(ParserContext context, SourceLine line, string id)
    {
        string? _screen;
        NavigationTargetSyntax? _target;

        /// <summary>
        /// Gets the first valid title, or <c>null</c> when no valid title was read.
        /// </summary>
        public string? Title { get; private set; }

        /// <summary>
        /// Gets the first icon, or <c>null</c> when none was read.
        /// </summary>
        public string? Icon { get; private set; }

        /// <summary>
        /// Gets the first valid slot, or <c>null</c> when no valid slot was read.
        /// </summary>
        public string? Slot { get; private set; }

        /// <summary>
        /// Reads the parts in authored order, retaining valid values while reporting errors.
        /// </summary>
        /// <param name="rest">The text following the item's id.</param>
        public void Read(string rest)
        {
            var valid = ReadParts(rest);
            if (Title is null && valid)
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Navigation item '{id}' has no title - expected 'item {id} title \"<text>\"'", line.Location);
            }
        }

        /// <summary>
        /// Applies the selected screen to the target, reporting a missing feature without discarding the target.
        /// </summary>
        /// <returns>The target, including the screen when its feature is explicit.</returns>
        public NavigationTargetSyntax? ResolveTarget()
        {
            if (_screen is null)
            {
                return _target;
            }

            if (_target?.Feature is null)
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Navigation item '{id}' selects screen '{_screen}' without an explicit feature - add 'target <Module>.<Feature>'", line.Location);
                return _target;
            }

            return _target with { Screen = _screen };
        }

        static bool IsBare(ItemPart part, Regex pattern) => !part.IsQuoted && !part.IsLocalized && pattern.IsMatch(part.Value);

        bool ReadParts(string rest)
        {
            var position = 0;
            var valid = true;
            while (position < rest.Length)
            {
                var match = PartRegex().Match(rest, position);
                if (!match.Success || match.Index != position)
                {
                    context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid navigation item '{line.Content}' - unexpected '{rest[position..].Trim()}'; expected {Expected}", line.Location);
                    return false;
                }

                position = match.Groups["end"].Index;
                var part = new ItemPart(match.Groups[1].Value, Text(match, 2, 3, 4), match.Groups[2].Success, match.Groups[3].Success);
                if (HasPart(part))
                {
                    context.Error(DiagnosticCodes.InvalidNavigationItem, $"Navigation item '{id}' states '{part.Key}' more than once", line.Location);
                    continue;
                }

                valid = ReadPart(part) && valid;
            }

            return valid;
        }

        bool HasPart(ItemPart part) => part.Key switch
        {
            "title" => Title is not null,
            "icon" => Icon is not null,
            "slot" => Slot is not null,
            "screen" => _screen is not null,
            _ => _target is not null
        };

        bool ReadPart(ItemPart part)
        {
            switch (part.Key)
            {
                case "title":
                    return ReadTitle(part);
                case "icon":
                    Icon = part.Value;
                    return true;
                case "screen":
                    _screen = ReadName(part);
                    return _screen is not null;
                case "slot":
                    Slot = ReadName(part);
                    return Slot is not null;
                default:
                    return ReadTarget(part);
            }
        }

        bool ReadTitle(ItemPart part)
        {
            if (!part.IsQuoted && !part.IsLocalized)
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid title '{part.Value}' on navigation item '{id}' - expected a quoted string or a $strings.<key> token", line.Location);
                return false;
            }

            Title = part.Value;
            return true;
        }

        string? ReadName(ItemPart part)
        {
            if (!IsBare(part, IdentifierRegex()))
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid {part.Key} '{part.Value}' on navigation item '{id}' - expected a {part.Key} name", line.Location);
                return null;
            }

            return part.Value;
        }

        bool ReadTarget(ItemPart part)
        {
            if (!IsBare(part, TargetRegex()))
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid target '{part.Value}' on navigation item '{id}' - expected <Module> or <Module>.<Feature>", line.Location);
                return false;
            }

            var dot = part.Value.IndexOf('.', StringComparison.Ordinal);
            _target = new(dot < 0 ? part.Value : part.Value[..dot], dot < 0 ? null : part.Value[(dot + 1)..], line.Location);
            return true;
        }
    }
}
