// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class PurposeValidator
{
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var purpose in application.Purposes)
        {
            if (!known.Add(purpose.Name)) context.Error(DiagnosticCodes.DuplicatePurposeDeclaration, $"Duplicate purpose '{purpose.Name}' - a purpose is declared once", purpose.Location);
        }

        foreach (var purpose in application.Purposes)
        {
            if (purpose.Interest is not null && purpose.Basis != "legitimateInterests")
            {
                context.Warning(DiagnosticCodes.PurposeInterestMismatch, $"Purpose '{purpose.Name}' declares an interest without basis legitimateInterests", purpose.Location);
            }
            else if (purpose.Basis == "legitimateInterests" && string.IsNullOrWhiteSpace(purpose.Interest))
            {
                context.Warning(DiagnosticCodes.PurposeInterestMismatch, $"Purpose '{purpose.Name}' with basis legitimateInterests has no interest statement (Art. 13(1)(d))", purpose.Location);
            }
        }

        var walker = new References(context, known);
        walker.VisitApplication(application);
    }

    sealed class References(ParserContext context, HashSet<string> known) : ScreenplaySyntaxWalker
    {
        public override void VisitPurposeReference(PurposeReferenceSyntax syntax)
        {
            if (!known.Contains(syntax.Name)) context.Warning(DiagnosticCodes.UnknownPurpose, $"Unknown purpose '{syntax.Name}' - declare it with 'purpose {syntax.Name}'", syntax.Location);
        }
    }
}
