// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed class PublicEventMetadataValidator(ParserContext context) : ScreenplaySyntaxWalker
{
    public override void VisitNode(SyntaxNode node)
    {
        if (PublicEventInvariants.Error(node) is not { } error) return;
        var code = node switch
        {
            EventSyntax => DiagnosticCodes.InvalidEventDeclaration,
            ImportSyntax => DiagnosticCodes.InvalidImportDeclaration,
            _ => DiagnosticCodes.InvalidSliceDeclaration
        };
        context.Error(code, error, node.DirectiveLocations.GetValueOrDefault("direction", node.Location));
    }
}
