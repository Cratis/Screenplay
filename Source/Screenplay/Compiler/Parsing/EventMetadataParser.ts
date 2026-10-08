// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseDocumentation } from './DocumentationParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const idPattern = pattern(`^id\\s+"(${stringBodyPattern})"$`);

export class EventMetadataParser {
    readonly value: { id: string | null; description: string | null; documentation: string | null } = { id: null, description: null, documentation: null };

    constructor(readonly name: string) {}

    tryParse(context: ParserContext, line: SourceLine): boolean {
        const location = locationOf(line);
        switch (firstWord(line.content)) {
            case 'id': {
                const match = idPattern.exec(line.content);
                const id = match === null ? '' : unescapeString(match[1]);
                if (id.trim().length === 0 || this.value.id !== null) {
                    context.error(DiagnosticCodes.InvalidEventId, `Event '${this.name}' accepts one nonempty 'id "<old-name>"'`, location);
                } else {
                    this.value.id = id;
                    if (id === this.name) {
                        context.information(DiagnosticCodes.RedundantEventId, `Event '${this.name}' already has this name as its identity - remove the redundant id`, location);
                    }
                }
                return true;
            }
            case 'description':
                this.value.description = parseDescription(context, line, this.value.description, `Event '${this.name}'`, true);
                return true;
            case 'documentation':
                this.value.documentation = parseDocumentation(context, line, this.value.documentation, `Event '${this.name}'`, DiagnosticCodes.InvalidEventDocumentation);
                return true;
            default:
                return false;
        }
    }
}
