// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { beforeAll, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { toCompleteSyntaxJson, toSyntaxJson } from '../../Syntax/SyntaxJson';
import { decodeExactSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';

class RecordingWalker extends ScreenplaySyntaxWalker {
    readonly nodes: SyntaxNode[] = [];
    override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
}

const source = readFileSync(resolve(__dirname, '../../Conformance/interactions.play'), 'utf8');

describe('when retaining interactions', () => {
    let application: ApplicationSyntax;
    let nodes: SyntaxNode[];

    beforeAll(() => {
        application = parse(source).value;
        const walker = new RecordingWalker();
        walker.visitApplication(application);
        nodes = walker.nodes;
    });

    it('should retain named behavior metadata and typed and untyped parameters', () => {
        const behavior = application.behaviors![0];
        behavior.name!.should.equal('ConfirmChanges');
        behavior.description!.should.equal('Confirm changes');
        behavior.order!.should.equal(-2);
        behavior.file!.path.should.equal('behaviors/confirm.ts');
        behavior.parameters.map(parameter => parameter.type?.name ?? null).should.deep.equal(['String', null]);
        behavior.bindings.should.have.lengthOf(5);
    });

    it('should preserve triggers and recursive action continuations', () => {
        const bindings = application.behaviors![0].bindings;
        bindings.map(binding => binding.trigger.kind).should.deep.equal(['BuiltInInteractionTriggerSyntax', 'BuiltInInteractionTriggerSyntax', 'EventInteractionTriggerSyntax', 'IntervalInteractionTriggerSyntax', 'ApplicationTriggerInteractionTriggerSyntax']);
        const confirm = bindings[0].actions[0];
        confirm.kind.should.equal('ConfirmActionSyntax');
        confirm.onSuccess[0].arguments[0].binding.should.equal('item.id');
        confirm.onSuccess[0].onFailure[0].kind.should.equal('NotifyActionSyntax');
        confirm.onFailure[0].kind.should.equal('NotifyActionSyntax');
    });

    it('should preserve ordered alternatives and fallback action lists', () => {
        const binding = application.behaviors![0].bindings[1];
        binding.actions.should.deep.equal([]);
        binding.alternatives.map(alternative => alternative.actions.map(action => action.kind)).should.deep.equal([
            ['ExecuteCommandActionSyntax', 'NavigateActionSyntax'], ['OpenDialogActionSyntax'],
        ]);
        binding.alternatives[1].actions[0].onResult.map(action => action.kind).should.deep.equal(['SetStateActionSyntax', 'CloseDialogActionSyntax']);
        binding.otherwise!.actions[0].kind.should.equal('NotifyActionSyntax');
    });

    it('should retain layout and module template attachments', () => {
        application.layouts![0].behaviors.should.have.lengthOf(1);
        application.layouts![0].usedBehaviors[0].arguments.map(argument => argument.value).should.deep.equal(['"Proceed?"', 'item.id']);
        const module = application.modules[0];
        module.screenTemplates![0].behaviors.should.have.lengthOf(1);
        module.screenTemplates![0].usedBehaviors.should.have.lengthOf(1);
        module.dialogTemplates![0].behaviors.should.have.lengthOf(1);
        module.dialogTemplates![0].usedBehaviors.should.have.lengthOf(1);
    });

    it('should retain screen wrappers and table and component interactions without swallowing siblings', () => {
        const directives = application.modules[0].features[0].slices[0].screens![0].directives;
        directives.map(directive => directive.kind).should.deep.equal(['ScreenBehaviorSyntax', 'ScreenUsesBehaviorSyntax', 'ScreenTableSyntax', 'ScreenComponentSyntax', 'ScreenTitleSyntax']);
        const [behavior, uses, table, component] = directives;
        if (behavior.kind !== 'ScreenBehaviorSyntax' || uses.kind !== 'ScreenUsesBehaviorSyntax' || table.kind !== 'ScreenTableSyntax' || component.kind !== 'ScreenComponentSyntax') throw new Error('Missing interaction owners');
        behavior.behavior.bindings.should.have.lengthOf(1);
        uses.uses.arguments[0].name.should.equal('message');
        table.behaviors![0].bindings[0].condition!.should.equal('item.enabled');
        table.usedBehaviors![0].arguments[1].value.should.equal('item.id');
        component.behaviors![0].bindings[0].actions[0].kind.should.equal('ExecuteCommandActionSyntax');
        component.usedBehaviors![0].behavior.should.equal('ConfirmChanges');
    });

    it('should walk the whole behavior in native order with one node visit per descendant', () => {
        const behavior = application.behaviors![0];
        const start = nodes.indexOf(behavior);
        nodes.slice(start, start + 8).map(node => node.kind).should.deep.equal(['BehaviorSyntax', 'FileReferenceSyntax', 'BehaviorParameterSyntax', 'TypeRefSyntax', 'BehaviorParameterSyntax', 'InteractionBindingSyntax', 'BuiltInInteractionTriggerSyntax', 'ConfirmActionSyntax']);
        nodes.filter(node => node.kind === 'InteractionAlternativeSyntax').should.have.lengthOf(2);
        nodes.filter(node => node.kind === 'BehaviorArgumentSyntax').should.have.lengthOf(12);
        nodes.filter(node => node === behavior.bindings[0].actions[0]).should.have.lengthOf(1);
        nodes.filter(node => node.kind === 'ComparisonConditionSyntax').should.have.lengthOf(2);
        new Set(nodes).size.should.equal(nodes.length);
    });

    it('should retain source locations on nested nodes', () => {
        const argument = application.behaviors![0].bindings[0].actions[0].onSuccess[0].arguments[0];
        argument.location.should.deep.equal({ line: source.split('\n').findIndex(line => line === '          with id from item.id') + 1, column: 11 });
    });

    it('should round trip the complete interaction tree through Exact JSON', () => {
        const json = toSyntaxJson(application);
        const restored = decodeExactSyntaxJson(JSON.stringify(json));
        toSyntaxJson(restored).should.deep.equal(json);
        toCompleteSyntaxJson(application).should.deep.equal(json);
    });

    it('should leave the legacy projection unchanged while retaining the complete tree', () => {
        const legacy = parse(source.replace('numbers exact\n', '')).value;
        const wire = toSyntaxJson(legacy) as Record<string, unknown>;
        Object.hasOwn(wire, 'behaviors').should.be.false;
        Object.hasOwn(wire, 'layouts').should.be.false;
        JSON.stringify(wire).includes('InteractionBindingSyntax').should.be.false;
        JSON.stringify(wire).includes('"kind":"UsesBehaviorSyntax"').should.be.false;
        const complete = toCompleteSyntaxJson(legacy) as Record<string, unknown>;
        (complete.behaviors as unknown[]).should.have.lengthOf(1);
    });

    it('should deserialize and re-encode complete transport from a Legacy-source tree without losing interactions', () => {
        const legacy = parse(source.replace('numbers exact\n', '')).value;
        const complete = toCompleteSyntaxJson(legacy);
        // The Exact decoder intentionally rejects Legacy source options. Its complete transport still
        // round trips through ordinary JSON, preserving the Legacy options and the whole typed graph.
        const restored = JSON.parse(JSON.stringify(complete)) as ApplicationSyntax;
        toCompleteSyntaxJson(restored).should.deep.equal(complete);
        toSyntaxJson(restored).should.deep.equal(toSyntaxJson(legacy));
        const walker = new RecordingWalker();
        walker.visitApplication(restored);
        walker.nodes.filter(node => node.kind === 'InteractionBindingSyntax').should.have.lengthOf(11);
        walker.nodes.filter(node => node.kind === 'BehaviorArgumentSyntax').should.have.lengthOf(12);
        restored.behaviors![0].bindings[0].actions[0].onSuccess[0].arguments[0].binding.should.equal('item.id');
    });
});
