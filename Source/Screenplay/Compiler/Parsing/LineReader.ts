// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isBlank, SourceLine } from './SourceLine';

// Walks the lines of a document forward. Significant reads skip blank lines; a raw read takes the next line
// as it is, which is how a fenced code block keeps its blank lines.
export class LineReader {
    #index = 0;

    constructor(private readonly lines: readonly SourceLine[]) {}

    peekSignificant(): SourceLine | undefined {
        while (this.#index < this.lines.length && isBlank(this.lines[this.#index])) {
            this.#index++;
        }
        return this.#index < this.lines.length ? this.lines[this.#index] : undefined;
    }

    takeSignificant(): SourceLine {
        const line = this.peekSignificant();
        if (line === undefined) {
            throw new Error('There is no significant line left to take');
        }
        this.#index++;
        return line;
    }

    takeRaw(): SourceLine | undefined {
        return this.#index < this.lines.length ? this.lines[this.#index++] : undefined;
    }
}
