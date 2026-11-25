// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import fs from 'node:fs';
import path from 'node:path';
import { describe, it } from 'vitest';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const serializeSyntax = (node: SyntaxNode): string => JSON.stringify(toSyntaxJson(node));
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import type { SyntaxNode } from '../Syntax/SyntaxNode';

const source = fs.readFileSync(path.join(__dirname, '../Conformance/layouts-templates-contributions.play'), 'utf8');

describe('when parsing layouts templates and contributions', () => {
    const result = parseForAuthoring(source, 'layouts.play', undefined, false);
    const application = result.value!;
    const layout = application.layouts![0];
    const module = application.modules[0];

    it('accepts the shared populated source', () => result.diagnostics.should.deep.equal([]));
    it('retains declaration slots separately from recursive slots', () => {
        layout.slots.map(slot => [slot.name, slot.role, slot.routed, slot.routedBy, slot.primary, slot.optional]).should.deep.equal([
            ['navigation', 'Panel', false, null, false, true], ['main', 'Page', true, 'record', true, false], ['aside', 'Pane', false, null, false, false]
        ]);
        layout.recursiveSlots!.map(slot => slot.name).should.deep.equal(['nested']);
    });
    it('retains every outlet kind in authored order', () => {
        layout.outlets!.map(outlet => outlet.syntaxKind).should.deep.equal(['Actions', 'Filters', 'Tabs', 'Stats', 'Navigation']);
        layout.outlets!.flatMap(outlet => outlet.items).every(item => item.isDefault).should.be.true;
        layout.outlets![4].logo!.title.should.equal('Workshop');
    });
    it('retains templates metadata and content', () => {
        const template = module.screenTemplates![0];
        template.fitsSlot!.should.equal('main');
        template.scopes!.should.deep.equal(['module', 'feature', 'slice']);
        template.sliceKinds!.should.deep.equal(['StateView']);
        template.displayName!.should.equal('Record list');
        template.description!.should.equal('A reusable list');
        template.content![0].directives[0].kind.should.equal('ScreenTitleSyntax');
        template.exposes![0].name.should.equal('selected');
    });
    it('retains arrangements and contribution metadata', () => {
        layout.arrangement!.root!.children[0].kind.should.equal('ArrangementContainerSyntax');
        layout.arrangement!.overrides![0].width!.should.equal('compact');
        module.dialogTemplates![0].arrangement!.variants![1].places[0].hidden!.should.be.true;
        module.contributions![1].navigate!.screen.should.equal('Overview');
        module.contributions![1].destination!.target.should.equal('detail');
        module.features[0].contributions![0].items[0].id.should.equal('inspect');
    });
    it('writes populated legacy syntax JSON without source-span metadata', () => {
        const json = serializeSyntax(application);
        json.includes('fitsSlotLocation').should.be.false;
        JSON.parse(json).layouts[0].outlets.map((outlet: { name: string }) => outlet.name).should.deep.equal(['actions', 'filters', 'tabs', 'stats', 'sidebar']);
    });
    it('visits populated layout children once and preserves locations', () => {
        const nodes: SyntaxNode[] = [];
        class Visitor extends ScreenplaySyntaxWalker { override visitNode(node: SyntaxNode): void { nodes.push(node); } }
        new Visitor().visitApplication(application);
        nodes.filter(node => node.kind === 'LayoutSyntax').length.should.equal(1);
        nodes.filter(node => node.kind === 'NavigationLogoSyntax').length.should.equal(1);
        nodes.filter(node => node.kind === 'PlaceSyntax').length.should.equal(2);
        nodes.every(node => node.location.path === 'layouts.play').should.be.true;
        new Set(nodes).size.should.equal(nodes.length);
    });
});

// Interaction bodies of templates keep their existing placeholder handling (#1388 tracks the full grammar):
// the facts are kept and validated, but their behavior is not claimed to round-trip.
describe('when parsing a template with interaction bodies', () => {
    const result = parseForAuthoring([
        'module Shop',
        '  screen template Listing',
        '    category Test',
        '    main',
        '    on load',
        '      refresh AllItems',
        '    uses Confirming'
    ].join('\n'), 'template.play', undefined, false);
    const template = result.value.modules[0].screenTemplates![0];

    it('reports no diagnostics', () => result.diagnostics.should.deep.equal([]));
    it('keeps the on behavior as a placeholder', () => template.behaviors!.length.should.equal(1));
    it('keeps the uses behavior as a placeholder', () => template.usedBehaviors!.length.should.equal(1));
    it('keeps the slot declared beside them', () => template.slots.map(slot => slot.name).should.deep.equal(['main']));
});
