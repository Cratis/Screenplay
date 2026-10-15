// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class DerivedGenerationRepairPattern
{
    internal static string? Create(SemanticGeneration? generation, out string? reason)
    {
        reason = null;
        if (generation?.Kind != "pattern")
        {
            var builtin = DerivedPattern.Create(generation);
            if (builtin is null) reason = "No bounded text shape is known for this generation strategy.";

            return builtin;
        }

        try
        {
            var shape = GenerationPattern.Parse(generation.Pattern!, generation.Checksum);
            var pattern = "^" + string.Concat(shape.Tokens.Select(Token)) + (shape.ChecksumWidth == 0 ? string.Empty : "[0-9]{" + shape.ChecksumWidth.ToString(CultureInfo.InvariantCulture) + "}") + "$";
            var expression = new RegexFragmentR(pattern, shape.MaximumLength, boundToShape: false).Parse();
            if (expression.HasNullableRepeat || !new GlushkovAutomaton(expression).IsDeterministic)
            {
                reason = "The derived matches expression is ambiguous; no deterministic bounded repair is available.";

                return null;
            }

            return pattern;
        }
        catch (RegexStateBudgetExceeded error)
        {
            reason = error.Message;
        }
        catch (OutsideRegexFragment error)
        {
            reason = error.Message;
        }
        catch (InvalidGenerationPattern error)
        {
            reason = error.Message;
        }

        return null;
    }

    static string Token(GenerationPatternToken token)
    {
        var atom = token switch
        {
            { Segment: not null } => "[0-9]{" + token.Width.ToString(CultureInfo.InvariantCulture) + "}",
            { Characters.Length: 1 } => DerivedPattern.Escape(token.Characters),
            _ => DerivedPattern.CharacterClass(token.Characters)
        };
        if (token.Minimum == 1 && token.Maximum == 1) return atom;
        var count = token.Minimum.ToString(CultureInfo.InvariantCulture) + (token.Maximum == token.Minimum ? string.Empty : "," + token.Maximum.ToString(CultureInfo.InvariantCulture));

        return "(?:" + atom + "){" + count + "}";
    }
}
