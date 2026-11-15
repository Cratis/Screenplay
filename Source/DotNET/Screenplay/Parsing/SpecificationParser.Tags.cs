// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    sealed class TagExpectations(ParserContext context, string? eventKeyword)
    {
        internal List<TagSyntax> Tags { get; } = [];
        internal SpecificationNoTagsSyntax? NoTags { get; private set; }

        internal bool TryParseValue(SourceLine line, Match mapping, List<PropertyMappingSyntax> values, IEnumerable<PropertyMappingSyntax>? other = null)
        {
            if (mapping.Success)
            {
                AddFixtureValue(context, values, ParseSpecificationMapping(context, mapping.Groups[1].Value, mapping.Groups[2], line), other);
                return true;
            }

            if (!IsTagDirective(line)) return false;
            Parse(line);
            return true;
        }

        static bool IsTagDirective(SourceLine line) => line.Content switch
        {
            "no tags" => true,
            "tag" => false,
            _ => LineText.FirstWord(line.Content) == "tag"
        };

        void Parse(SourceLine line)
        {
            if (eventKeyword != "then")
            {
                context.Error(DiagnosticCodes.InvalidEventTagExpectation, "Tags are asserted only on a 'then' event; a 'given', 'when', redelivery or example occurrence cannot declare 'tag' or 'no tags'.", line.Location);
                context.SkipBlock(line.Indent);
                return;
            }

            if (line.Content == "no tags") ParseNoTags(line);
            else ParseTag(line);
        }

        void ParseNoTags(SourceLine line)
        {
            if (NoTags is not null || Tags.Count > 0)
            {
                context.Error(DiagnosticCodes.InvalidEventTagExpectation, "'no tags' cannot be repeated or combined with 'tag' lines.", line.Location);
            }
            else
            {
                NoTags = new(line.Location);
            }

            RejectChildren(line);
        }

        void ParseTag(SourceLine line)
        {
            RejectChildren(line);
            if (NoTags is not null)
            {
                context.Error(DiagnosticCodes.InvalidEventTagExpectation, "'tag' cannot be combined with 'no tags'.", line.Location);
                return;
            }

            if (TagParser.Parse(context, line) is not { } tag) return;
            if (IsExpectedLiteral(tag, line)) Tags.Add(tag);
        }

        bool IsExpectedLiteral(TagSyntax tag, SourceLine line)
        {
            if (tag.Value is not LiteralExpressionSyntax literal)
            {
                context.Error(DiagnosticCodes.InvalidEventTagExpectation, "An expected tag is a literal - an identifier or a string; a context expression is resolved at append time and cannot be asserted.", line.Location);
                return false;
            }

            if (literal.Value is string value && !string.IsNullOrWhiteSpace(value)) return true;
            context.Error(DiagnosticCodes.InvalidEventTagExpectation, "An expected tag is a nonblank string literal or identifier.", line.Location);
            return false;
        }

        void RejectChildren(SourceLine line)
        {
            if (context.TryPeekChild(line.Indent, out var child))
            {
                context.Error(DiagnosticCodes.InvalidEventTagExpectation, "A tag expectation cannot have children.", child.Location);
                context.SkipBlock(line.Indent);
            }
        }
    }
}
