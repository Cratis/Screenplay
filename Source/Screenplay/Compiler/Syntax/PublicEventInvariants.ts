// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax, ImportSyntax } from './Declarations';
import { EventVisibility } from './EventVisibility';
import { SliceSyntax } from './Structure';
import { SyntaxNode } from './SyntaxNode';
import { TranslationDirection } from './TranslationDirection';
import { dotNetWhitespace } from '../Text/patterns';

// .NET whitespace differs from ECMAScript trim at NEL and BOM. Never normalize opaque text.
const blankOrigin = new RegExp(`^${dotNetWhitespace}*$`);
export const isBlankPublicEventOrigin = (text: string): boolean => blankOrigin.test(text);

// The native PublicEventInvariants contract, shared by source and transport validation.
export function publicEventMetadataError(node: SyntaxNode): string | undefined {
    if (node.kind === 'EventSyntax' || node.kind === 'ImportSyntax') {
        const declaration = node as EventSyntax | ImportSyntax;
        const visibility = declaration.visibility === undefined ? EventVisibility.Private : declaration.visibility;
        if (visibility !== EventVisibility.Private && visibility !== EventVisibility.Public) return 'Event visibility must be Private or Public.';
        if (declaration.origin != null && (typeof declaration.origin !== 'string' || isBlankPublicEventOrigin(declaration.origin))) return 'An event or import origin must be a nonblank quoted string.';
        if (declaration.origin != null && visibility !== EventVisibility.Public) return 'An event or import with an origin is a public contract.';
        if (node.kind === 'ImportSyntax' && visibility === EventVisibility.Public && declaration.origin == null) return 'A public contract import requires an origin.';
    }
    if (node.kind === 'SliceSyntax') {
        const slice = node as SliceSyntax;
        if (slice.direction != null && slice.type !== 'Translate') return 'Only a Translate slice may declare a direction.';
        if (slice.direction != null && slice.direction !== TranslationDirection.Inbound && slice.direction !== TranslationDirection.Outbound) return 'A translation direction must be Inbound or Outbound.';
    }
    return undefined;
}
