// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { LineReader } from '../../Parsing/LineReader';
import { ParserContext } from '../../Parsing/ParserContext';
import { validateSpecificationStreams } from '../../Parsing/SpecificationStreamValidator';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { ApplicationSyntax } from '../Structure';
import { decodeExactSyntaxJson } from '../StrictSyntaxJson';
import { SyntaxNode } from '../SyntaxNode';
import { toSyntaxJson } from '../SyntaxJson';

for (const [name, event] of [
    ['omitted route members', { kind: 'SpecificationEventSyntax', eventType: 'E', for: null, values: [] }],
    ['C# emitted null route members', JSON.parse(readFileSync(join(__dirname, '../../Conformance/specification-event-without-route.json'), 'utf8'))],
] as const) {
    describe(`when walking route free exact specifications with ${name}`, () => {
        let visited: string[];
        let context: ParserContext;
        beforeEach(() => {
            const wire = toSyntaxJson(parse('numbers exact\nmodule M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        given E\n        when append E\n        then E').value!) as unknown as ApplicationSyntax;
            Object.assign(wire.modules[0].features[0].slices[0].specifications[0], { given: [event], whenAppended: event, thenEvents: [event] });
            const model = decodeExactSyntaxJson(JSON.stringify(wire)) as ApplicationSyntax;
            visited = [];
            new class extends ScreenplaySyntaxWalker {
                visitNode(node: SyntaxNode): void { visited.push(node.kind); }
            }().visitApplication(model);
            context = new ParserContext(new LineReader([]));
            validateSpecificationStreams(model, context);
        });
        it('should walk all event occurrences without inventing a route', () => visited.filter(kind => kind === 'SpecificationEventSyntax').should.have.lengthOf(3));
        it('should validate without routing diagnostics', () => context.diagnostics.should.deep.equal([]));
    });
}
