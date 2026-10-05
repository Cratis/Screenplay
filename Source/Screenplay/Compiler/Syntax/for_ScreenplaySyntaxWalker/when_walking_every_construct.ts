// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { SyntaxJsonValue, toCompleteSyntaxJson } from '../SyntaxJson';
import { SyntaxNode } from '../SyntaxNode';

class KindCounter extends ScreenplaySyntaxWalker {
    readonly kinds = new Map<string, number>();

    override visitNode(node: SyntaxNode): void {
        this.kinds.set(node.kind, (this.kinds.get(node.kind) ?? 0) + 1);
    }
}

function kindsIn(value: SyntaxJsonValue, counted = new Map<string, number>()): Map<string, number> {
    if (Array.isArray(value)) {
        value.forEach(item => kindsIn(item, counted));
    } else if (value !== null && typeof value === 'object') {
        if (typeof value.kind === 'string') {
            counted.set(value.kind, (counted.get(value.kind) ?? 0) + 1);
        }
        Object.values(value).forEach(member => kindsIn(member, counted));
    }
    return counted;
}

// The walker exists so a consumer never writes the walk itself, which only holds if it reaches every node.
// The canonical JSON holds every node the tree has, so the two must count the same of every kind.
describe('when walking every construct', () => {
    let walked: Map<string, number>;
    let inTree: Map<string, number>;

    beforeAll(() => {
        const application = parse(readFileSync(resolve(__dirname, '../../Conformance/constructs.play'), 'utf8')).value;
        const counter = new KindCounter();
        counter.visitApplication(application);
        walked = counter.kinds;
        inTree = kindsIn(toCompleteSyntaxJson(application));
    });

    it('should find many kinds of node to walk', () => {
        inTree.size.should.be.greaterThan(40);
    });

    it('should visit every node of every kind once', () => {
        Object.fromEntries([...walked].sort()).should.deep.equal(Object.fromEntries([...inTree].sort()));
    });
});

// The invoicing sample holds what the construct corpus cannot - a capture, which the C# semantic model
// does not bind yet - so it is walked as well.
describe('when walking the invoicing sample', () => {
    let walked: Map<string, number>;
    let inTree: Map<string, number>;

    beforeAll(() => {
        const application = parse(readFileSync(resolve(__dirname, '../../../Monaco/screenplay-editor/samples/invoicing.play'), 'utf8')).value;
        const counter = new KindCounter();
        counter.visitApplication(application);
        walked = counter.kinds;
        inTree = kindsIn(toCompleteSyntaxJson(application));
    });

    it('should find a capture to walk', () => (inTree.get('CaptureSyntax') ?? 0).should.be.greaterThan(0));

    it('should visit every node of every kind once', () => {
        Object.fromEntries([...walked].sort()).should.deep.equal(Object.fromEntries([...inTree].sort()));
    });
});
