// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { SyntaxNode } from '../SyntaxNode';
import { IdentitySourceSyntax } from '../IdentitySourceSyntax';

class VisitedNodes extends ScreenplaySyntaxWalker {
    readonly kinds: string[] = [];
    override visitNode(node: SyntaxNode): void { this.kinds.push(node.kind); }
}
const location = { line: 1, column: 1 };

describe('when visiting explicit authoring children', () => {
    it('should visit reducer filters and both realization forms in order', () => {
        const walker = new VisitedNodes();
        walker.visitReducer({ kind: 'ReducerSyntax', name: 'R', readModel: 'View', description: null,
            from: { kind: 'ObserverFilterSyntax', eventSource: 'Account', stream: 'All', location },
            rules: [
                { kind: 'ReducerRuleSyntax', event: 'First', description: null, file: { kind: 'FileReferenceSyntax', path: 'First.cs', location }, code: null, location },
                { kind: 'ReducerRuleSyntax', event: 'Second', description: null, file: null, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'return context.State;', location }, location },
            ], location });
        expect(walker.kinds).toEqual(['ReducerSyntax', 'ObserverFilterSyntax', 'ReducerRuleSyntax', 'FileReferenceSyntax', 'ReducerRuleSyntax', 'CodeBlockSyntax']);
    });
    it('should visit form fields and submit navigation', () => {
        const walker = new VisitedNodes();
        walker.visitForm({ kind: 'FormSyntax', name: 'Edit', for: 'C', populate: null,
            fields: [{ kind: 'FormFieldSyntax', property: 'value', label: null, from: null, composeUsing: null, location }],
            onSubmit: { kind: 'ScreenNavigateSyntax', screen: 'Details', by: 'id', route: null, parameters: [], location }, location });
        expect(walker.kinds).toEqual(['FormSyntax', 'FormFieldSyntax', 'ScreenNavigateSyntax']);
    });
    it('should visit additional constraint rules and specification-local examples', () => {
        const slice = parse('module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n      event Changed\n        value String\n      constraint Once\n        unique event Changed\n      example Existing : Changed\n        value = "kept"\n      specification Scenario\n        when C\n          value = "kept"').value.modules[0].features[0].slices[0];
        const walker = new VisitedNodes();
        const rule = slice.constraints[0];
        walker.visitConstraint({ ...rule, additionalRules: [{ ...rule, name: 'Child', additionalRules: [] }] });
        walker.visitSpecification({ ...slice.specifications[0], examples: slice.examples });
        expect(walker.kinds.filter(kind => kind === 'UniqueEventConstraintSyntax')).toHaveLength(2);
        expect(walker.kinds.filter(kind => kind === 'SpecificationExampleSyntax')).toHaveLength(1);
        expect(walker.kinds).toContain('PropertyMappingSyntax');
    });
    it('should retain the unknown-form fallback for a future identity source', () => {
        const walker = new VisitedNodes();
        walker.visitIdentitySource({ kind: 'FutureIdentitySourceSyntax', location } as unknown as IdentitySourceSyntax);
        expect(walker.kinds).toEqual(['FutureIdentitySourceSyntax']);
    });
});
