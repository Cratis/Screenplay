// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ParserContext } from './ParserContext';

// Private construction metadata: leaves have depth one; grouping does not add a syntax node.
export class ConditionDepthBudget {
    readonly #depths = new WeakMap<object, number>();
    #nesting = 0;
    exceeded = false;

    constructor(private readonly context: ParserContext, private readonly location: SourceLocation) {}

    enter(): boolean {
        if (this.#nesting === 128) return this.#refuse();
        this.#nesting++;
        return true;
    }

    leave(): void { this.#nesting--; }

    build<T extends object>(children: readonly object[], create: () => T): T | null {
        const depth = 1 + Math.max(...children.map(child => this.#depths.get(child) ?? 1));
        if (depth > 128) {
            this.#refuse();
            return null;
        }
        const node = create();
        this.#depths.set(node, depth);
        return node;
    }

    #refuse(): false {
        if (!this.exceeded) {
            this.context.error(DiagnosticCodes.ConditionDepthExceeded, 'Condition nesting or expression depth exceeds the supported limit of 128.', this.location);
            this.exceeded = true;
        }
        return false;
    }
}
