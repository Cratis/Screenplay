// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ValidationRuleParser
{
    static (ValidationRuleKind? Kind, ExpressionSyntax? Value) ParseChecksum(ParserContext context, string rule, SourceLine line)
    {
        var algorithm = rule["checksum".Length..].Trim();
        if (algorithm is not ("luhn" or "mod11" or "mod97"))
        {
            context.Error(DiagnosticCodes.InvalidGenerationArgument, "Checksum validation requires luhn, mod11 or mod97.", line.Location);
            return (null, null);
        }

        return (ValidationRuleKind.Checksum, new LiteralExpressionSyntax(algorithm, line.Location));
    }
}
