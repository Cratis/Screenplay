// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { InteractionActionSyntax, InteractionTriggerSyntax } from '../../Syntax/Interactions';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { assertExactSyntaxRoundTrip } from './given/assert_exact_syntax_round_trip';

class RecordingWalker extends ScreenplaySyntaxWalker {
    readonly nodes: SyntaxNode[] = [];
    override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
}

const triggers = ['click', 'double click', 'select', 'submit', 'change', 'load', 'unload', 'enter', 'leave', 'event Work.Changed', 'interval 2 seconds', 'interval 3 minutes', 'interval 4 hours', 'interval 5 days', 'Work.Tick', 'chosen'];
const source = `numbers exact\nbehavior Triggers\n${triggers.map(trigger => `  on ${trigger}\n    navigate back`).join('\n')}`;

describe('when retaining interaction triggers', () => {
    let parsed: readonly InteractionTriggerSyntax[];

    beforeAll(() => { parsed = parse(source).value.behaviors![0].bindings.map(binding => binding.trigger); });

    it('should retain each built-in trigger using its native enum member', () => {
        parsed.slice(0, 9).map(trigger => trigger.kind === 'BuiltInInteractionTriggerSyntax' ? trigger.syntaxKind : null).should.deep.equal(['Click', 'DoubleClick', 'Select', 'Submit', 'Change', 'Load', 'Unload', 'Enter', 'Leave']);
    });

    it('should retain qualified event names', () => {
        const event = parsed[9];
        if (event.kind !== 'EventInteractionTriggerSyntax') throw new Error('Missing event trigger');
        event.eventName.should.equal('Work.Changed');
    });

    it('should retain interval amounts and normalized units', () => {
        parsed.slice(10, 14).map(trigger => trigger.kind === 'IntervalInteractionTriggerSyntax' ? [trigger.amount, trigger.unit] : null).should.deep.equal([[2, 'Seconds'], [3, 'Minutes'], [4, 'Hours'], [5, 'Days']]);
    });

    it('should retain application triggers and component signals as named triggers', () => {
        parsed.slice(14).map(trigger => trigger.kind === 'ApplicationTriggerInteractionTriggerSyntax' ? trigger.triggerName : null).should.deep.equal(['Work.Tick', 'chosen']);
    });

    it('should keep the native visitor fallback for a future trigger subtype', () => {
        // The native walker visits unknown derived nodes even though the wire protocol is closed.
        const trigger = { kind: 'FutureInteractionTriggerSyntax', location: sourceLocation(1, 1) } as unknown as InteractionTriggerSyntax;
        const walker = new RecordingWalker();
        walker.visitInteractionTrigger(trigger);
        walker.nodes.should.deep.equal([trigger]);
    });

    it('should visit a future action subtype once and still traverse its continuation', () => {
        const child: InteractionActionSyntax = { kind: 'NavigateBackActionSyntax', arguments: [], onSuccess: [], onFailure: [], onResult: [], location: sourceLocation(2, 1) };
        const action = { kind: 'FutureInteractionActionSyntax', arguments: [], onSuccess: [child], onFailure: [], onResult: [], location: sourceLocation(1, 1) } as unknown as InteractionActionSyntax;
        const walker = new RecordingWalker();
        walker.visitInteractionAction(action);
        walker.nodes.should.deep.equal([action, child]);
    });

    it('should round trip every trigger subtype through Exact JSON', () => {
        const json = toSyntaxJson(parse(source).value);
        assertExactSyntaxRoundTrip(json);
    });
});
