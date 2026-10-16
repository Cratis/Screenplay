// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public static partial class GenerationStrategyCatalog
{
    internal static void ValidateValues(string primitive, GenerationSyntax generation, Action<Diagnostic, string?> error)
    {
        var strategy = generation.Strategy;
        if (strategy == "pattern" && generation.Pattern is { } pattern)
        {
            try
            {
                _ = GenerationPattern.Parse(pattern, generation.Checksum);
            }
            catch (InvalidGenerationPattern failure)
            {
                var code = failure.Message.Contains("expanded length", StringComparison.Ordinal) ? DiagnosticCodes.GenerationSizeOutsideRange : DiagnosticCodes.InvalidGenerationArgument;
                error(Diagnostic.Error(code, failure.Message, SourceLocation.Start), "pattern");
            }
        }
        if (!AllowsPrimitive(strategy, primitive)) error(Diagnostic.Error(DiagnosticCodes.InvalidGenerationPrimitive, $"Generation strategy '{strategy}' is not allowed on '{primitive}'.", SourceLocation.Start), null);
        if (generation.Prefix is { } prefix)
        {
            if (strategy == "typeid")
            {
                if (!ValidText(OptionFor("typeid", "prefix"), prefix)) error(Diagnostic.Error(DiagnosticCodes.InvalidTypeIdPrefix, "TypeID prefix must be empty or 1..63 lowercase letters and underscores, starting and ending with a letter.", SourceLocation.Start), "prefix");
            }
            else if (!ValidText(OptionFor("uuid", "prefix"), prefix))
            {
                error(Diagnostic.Error(DiagnosticCodes.InvalidGenerationPrefix, "Generation prefix must contain at most 32 printable ASCII characters.", SourceLocation.Start), "prefix");
            }
        }

        if (generation.Alphabet is { } alphabet && !ValidText(OptionFor("nanoid", "alphabet"), alphabet))
        {
            error(Diagnostic.Error(DiagnosticCodes.InvalidGenerationAlphabet, "NanoID alphabet must contain 2..256 distinct printable ASCII characters.", SourceLocation.Start), "alphabet");
        }

        if ((generation.Size is { } size && !InRange(OptionFor("nanoid", "size"), size)) || (generation.Count is { } count && !InRange(OptionFor("bytes", "count"), count)))
        {
            error(Diagnostic.Error(DiagnosticCodes.GenerationSizeOutsideRange, "NanoID size must be 1..256 and bytes count must be 1..512.", SourceLocation.Start), generation.Size is { } length && !InRange(OptionFor("nanoid", "size"), length) ? "size" : "count");
        }
    }
}
