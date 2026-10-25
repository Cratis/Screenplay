// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class ActionSubjectInvariants
{
    internal static void Validate(SyntaxNode node)
    {
        if (node is ToolbarItemSyntax { Subject: not null, Kind: not ToolbarItemKind.Action })
        {
            throw new InvalidSyntaxJson("Only an action toolbar item can have a subject.");
        }
    }
}
