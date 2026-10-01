// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { SourceLocation, sourceLocation } from '../Diagnostics/SourceLocation';
import { LineReader } from './LineReader';
import { SourceLine } from './SourceLine';

// What every parser shares while it reads one document: the line reader, the diagnostics found so far and
// the file the document came from.
export class ParserContext {
    readonly #diagnostics: Diagnostic[] = [];

    constructor(readonly reader: LineReader, readonly path?: string) {}

    get start(): SourceLocation {
        return sourceLocation(1, 1, this.path);
    }

    get diagnostics(): readonly Diagnostic[] {
        return this.#diagnostics;
    }

    error(code: string, message: string, location: SourceLocation): void {
        this.#diagnostics.push({ severity: 'error', code, message, location });
    }

    warning(code: string, message: string, location: SourceLocation): void {
        this.#diagnostics.push({ severity: 'warning', code, message, location });
    }

    // The next significant line when it is indented deeper than its parent - that is, when it belongs to
    // the parent's body under the offside rule.
    peekChild(parentIndent: number): SourceLine | undefined {
        const line = this.reader.peekSignificant();
        return line !== undefined && line.indent > parentIndent ? line : undefined;
    }

    // Skips the body of a construct, the way the C# parser does after reporting it.
    skipBlock(parentIndent: number): void {
        while (this.peekChild(parentIndent) !== undefined) {
            this.reader.takeSignificant();
        }
    }

    // Skips the body of a construct this compiler deliberately does not model. Unlike skipBlock it steps
    // over fenced code blocks whole, because a fenced line may be indented less than the block it is in -
    // reading it as significant would end the skip early and misread the code as Screenplay.
    skipOpaqueBlock(parentIndent: number): void {
        let child = this.peekChild(parentIndent);
        while (child !== undefined) {
            this.reader.takeSignificant();
            if (child.content.startsWith('```')) {
                this.skipFencedBody();
            }
            child = this.peekChild(parentIndent);
        }
    }

    // Takes the raw lines of a fenced block up to and including its closing fence.
    skipFencedBody(): void {
        for (let line = this.reader.takeRaw(); line !== undefined; line = this.reader.takeRaw()) {
            if (line.raw.trim() === '```') {
                return;
            }
        }
    }
}
