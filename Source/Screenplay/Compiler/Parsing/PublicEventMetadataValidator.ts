// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { publicEventMetadataError } from '../Syntax/PublicEventInvariants';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ParserContext } from './ParserContext';

class PublicEventMetadataValidator extends ScreenplaySyntaxWalker {
    constructor(private readonly context: ParserContext) { super(); }

    override visitNode(node: SyntaxNode): void {
        const error = publicEventMetadataError(node);
        if (error === undefined) return;
        const code = node.kind === 'EventSyntax' ? DiagnosticCodes.InvalidEventDeclaration
            : node.kind === 'ImportSyntax' ? DiagnosticCodes.InvalidImportDeclaration : DiagnosticCodes.InvalidSliceDeclaration;
        this.context.error(code, error, node.location);
    }
}

export function validatePublicEventMetadata(application: ApplicationSyntax, context: ParserContext): void {
    new PublicEventMetadataValidator(context).visitApplication(application);
}
