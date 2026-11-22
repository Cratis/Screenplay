// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { invalidActionPresentation } from '../Syntax/ScreenActionPresentation';
import { InteractionArgumentSyntax, PresentationValueSyntax, ScreenActionSubjectKind, ScreenActionSyntax, ScreenNavigateSyntax } from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parsePlainActionInput } from './ActionSubjectClause';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

interface PlainActionHeader {
    readonly command: string;
    readonly label?: string;
    readonly subject?: ScreenActionSubjectKind;
}

interface PlainActionParsers {
    readonly navigate: (context: ParserContext, content: string, line: SourceLine) => ScreenNavigateSyntax | undefined;
    readonly presentation: (context: ParserContext, line: SourceLine, values: PresentationValueSyntax[]) => void;
}

const label = pattern(`^label\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))$`);

export function parsePlainScreenAction(context: ParserContext, line: SourceLine, header: PlainActionHeader, parsers: PlainActionParsers): ScreenActionSyntax {
    const body = new PlainActionBody(header.label ?? null);
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        body.read(context, child, parsers);
    }
    return body.build(context, line, header);
}

class PlainActionBody {
    private text: string | null;
    private target: ScreenNavigateSyntax | null = null;
    private readonly inputs: InteractionArgumentSyntax[] = [];
    private readonly presentation: PresentationValueSyntax[] = [];

    constructor(text: string | null) { this.text = text; }

    read(context: ParserContext, child: SourceLine, parsers: PlainActionParsers): void {
        const match = label.exec(child.content);
        if (match !== null) {
            this.text = match[1] !== undefined ? unescapeString(match[1]) : match[2];
            return;
        }
        this.readDirective(context, child, parsers);
    }

    build(context: ParserContext, line: SourceLine, header: PlainActionHeader): ScreenActionSyntax {
        const reason = invalidActionPresentation(this.presentation, header.subject);
        if (reason !== undefined) context.error(DiagnosticCodes.InvalidActionPresentation, reason, locationOf(line));
        return { kind: 'ScreenActionSyntax', command: header.command, label: this.text, navigate: this.target,
            ...(this.inputs.length === 0 ? {} : { inputs: this.inputs }),
            ...(this.presentation.length === 0 ? {} : { presentation: this.presentation }), location: locationOf(line) };
    }

    private readDirective(context: ParserContext, child: SourceLine, parsers: PlainActionParsers): void {
        switch (firstWord(child.content)) {
            case 'navigate':
                this.target = parsers.navigate(context, child.content, child) ?? null;
                break;
            case 'with': {
                const input = parsePlainActionInput(context, child, this.inputs);
                if (input !== undefined) this.inputs.push(input);
                break;
            }
            case 'presentation':
                parsers.presentation(context, child, this.presentation);
                break;
            default:
                context.error(DiagnosticCodes.UnknownActionDirective, `Unexpected '${child.content}' in action - expected 'label "..."' or 'navigate to ...'`, locationOf(child));
        }
    }
}
