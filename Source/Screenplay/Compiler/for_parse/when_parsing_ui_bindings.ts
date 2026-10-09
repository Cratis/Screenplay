// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { ParserContext } from '../Parsing/ParserContext';
import { parseFromClause, parseUiBinding } from '../Parsing/UiBindingParser';
import { UiBindingSyntax } from '../Syntax/Screens';

const location = { line: 1, column: 1 };

describe('when parsing UI bindings directly', () => {
    let diagnostics: string[];
    let context: ParserContext;

    beforeEach(() => {
        diagnostics = [];
        context = { error: (_code: string, message: string) => diagnostics.push(message) } as unknown as ParserContext;
    });

    it('should parse from-clause data bindings', () => {
        const binding = parseFromClause(context, 'data selected.id', location);
        binding.should.deep.include({ bindingKind: 'DataContext', path: 'selected.id' });
    });

    it('should parse legacy data bindings', () => {
        const binding = parseFromClause(context, 'selected.id', location);
        binding.should.deep.include({ bindingKind: 'DataContext', path: 'selected.id' });
    });

    it('should parse query bindings', () => {
        const binding = parseUiBinding(context, 'from query Invoices.selected.id', location);
        binding.should.deep.include({ bindingKind: 'QueryResult', query: 'Invoices', path: 'selected.id' });
    });

    it('should parse component bindings', () => {
        const binding = parseUiBinding(context, 'from component grid.selected.id mode twoWay null preserve expected String', location);
        binding.should.deep.include({ bindingKind: 'ComponentProperty', componentId: 'grid', componentPropertyPath: 'selected.id', mode: 'TwoWay', nullBehavior: 'Preserve', expectedValueType: 'String' });
    });

    it('should preserve unknown typed source text', () => {
        const headless = parseUiBinding(context, 'from ?', location);
        headless.rawText!.should.equal('from ?');
        const binding = parseUiBinding(context, 'from query 42', location);
        binding.should.deep.include({ bindingKind: 'Invalid', rawText: 'from query 42' });
        diagnostics.should.have.lengthOf(2);
    });

    it('should preserve unsupported modifiers', () => {
        const binding = parseUiBinding(context, 'from data selected carry forever', location) as UiBindingSyntax;
        binding.rawText!.should.equal('carry forever');
        diagnostics.should.have.lengthOf(1);
    });
});
