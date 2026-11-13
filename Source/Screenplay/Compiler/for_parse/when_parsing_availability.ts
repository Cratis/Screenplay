// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { AvailableSyntax, availabilityLeaves } from '../Syntax/Availability';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';

const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);

class Gates extends ScreenplaySyntaxWalker {
    readonly owners: string[] = [];
    override visitModule(syntax: Parameters<ScreenplaySyntaxWalker['visitModule']>[0]): void { this.#note('module', syntax.available); super.visitModule(syntax); }
    override visitFeature(syntax: Parameters<ScreenplaySyntaxWalker['visitFeature']>[0]): void { this.#note('feature', syntax.available); super.visitFeature(syntax); }
    override visitSlice(syntax: Parameters<ScreenplaySyntaxWalker['visitSlice']>[0]): void { this.#note('slice', syntax.available); super.visitSlice(syntax); }
    override visitCommand(syntax: Parameters<ScreenplaySyntaxWalker['visitCommand']>[0]): void { this.#note('command', syntax.available); super.visitCommand(syntax); }
    override visitQuery(syntax: Parameters<ScreenplaySyntaxWalker['visitQuery']>[0]): void { this.#note('query', syntax.available); super.visitQuery(syntax); }
    override visitScreen(syntax: Parameters<ScreenplaySyntaxWalker['visitScreen']>[0]): void { this.#note('screen', syntax.available); super.visitScreen(syntax); }
    #note(owner: string, gates: readonly AvailableSyntax[] | undefined): void {
        for (const gate of gates ?? []) this.owners.push(`${owner}:${gate.requirement.kind}`);
    }
}

describe('when parsing availability', () => {
    it('should retain audiences, availabilities and persona audiences', () => {
        const result = parse([
            'audience Plus',
            '  description "Customers on Plus"',
            'audience Preview',
            'availability Everything',
            '  description "Anything sold"',
            '  for Plus or Preview',
            '  when $context.tier == "gold"',
            '  with Other',
            'availability Other',
            'persona Manager',
            '  audience Plus',
            ''
        ].join('\n'));
        result.value.audiences!.map(audience => [audience.name, audience.description ?? null]).should.deep.equal([['Plus', 'Customers on Plus'], ['Preview', null]]);
        result.value.availabilities!.map(availability => availability.requirements.map(requirement => requirement.kind)).should.deep.equal([
            ['LogicalAvailabilityRequirementSyntax', 'ConditionAvailabilityRequirementSyntax', 'AvailabilityReferenceSyntax'],
            []
        ]);
        result.value.personas[0].audiences!.should.deep.equal(['Plus']);
    });

    it('should omit audiences and availabilities that are not declared', () => {
        const result = parse('module M\n');
        (result.value.audiences === undefined).should.be.true;
        (result.value.availabilities === undefined).should.be.true;
    });

    it('should retain a gate on every construct that takes one', () => {
        const result = parse([
            'audience A',
            'module M',
            '  available for A',
            '  feature F',
            '    available for A',
            '    slice StateChange S',
            '      available for A',
            '      event E',
            '        x String',
            '      command C',
            '        available for A',
            '        x String',
            '        produces E',
            '          x = x',
            '      screen Sc',
            '        available for A',
            '    slice StateView V',
            '      query Q => R',
            '        available for A',
            '        by id Uuid',
            '      readmodel R',
            '        id Uuid',
            ''
        ].join('\n'));
        const gates = new Gates();
        gates.visitApplication(result.value);
        gates.owners.should.deep.equal([
            'module:AudienceAvailabilityRequirementSyntax',
            'feature:AudienceAvailabilityRequirementSyntax',
            'slice:AudienceAvailabilityRequirementSyntax',
            'command:AudienceAvailabilityRequirementSyntax',
            'screen:AudienceAvailabilityRequirementSyntax',
            'query:AudienceAvailabilityRequirementSyntax'
        ]);
    });

    it('should bind and tighter than or and let parentheses group', () => {
        const result = parse('audience A\naudience B\naudience C\nmodule M\n  available for A or B and C\n  available for (A or B) and C\n');
        const [loose, grouped] = result.value.modules[0].available!;
        const shape = (gate: AvailableSyntax): string => gate.requirement.kind === 'LogicalAvailabilityRequirementSyntax'
            ? `${gate.requirement.operator}:${gate.requirement.left.kind === 'LogicalAvailabilityRequirementSyntax' ? 'logical' : 'leaf'}/${gate.requirement.right.kind === 'LogicalAvailabilityRequirementSyntax' ? 'logical' : 'leaf'}`
            : 'leaf';
        shape(loose).should.equal('Or:leaf/logical');
        shape(grouped).should.equal('And:logical/leaf');
        availabilityLeaves(grouped.requirement, 'AudienceAvailabilityRequirementSyntax').map(leaf => leaf.audience).should.deep.equal(['A', 'B', 'C']);
    });

    it('should keep an available property of a command when it is not a gate', () => {
        const result = parse(['module M', '  feature F', '    slice StateChange S', '      event E', '        x String', '      command C', '        available Bool', '        produces E', '          x = x', ''].join('\n'));
        codes('module M\n').should.deep.equal([]);
        (result.value.modules[0].features[0].slices[0].commands[0].available === undefined).should.be.true;
        result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name).should.deep.equal(['available']);
    });

    it('should report a malformed declaration or gate', () => {
        codes('audience\n').should.deep.equal(['PLAY1130@1']);
        codes('availability\n').should.deep.equal(['PLAY1131@1']);
        codes('module M\n  available for\n').should.deep.equal(['PLAY1132@2']);
        codes('audience A\nmodule M\n  available for (A\n').should.deep.equal(['PLAY1141@3']);
        codes('audience A\naudience B\nmodule M\n  available for A B\n').should.deep.equal(['PLAY1140@4']);
    });

    it('should report duplicates, unknown names and cycles', () => {
        codes('audience A\naudience A\n').should.deep.equal(['PLAY1133@2']);
        codes('module M\n  available for Ghost\n').should.deep.equal(['PLAY1135@2']);
        codes('module M\n  available with Ghost\n').should.deep.equal(['PLAY1136@2']);
        codes('availability A\n  with B\navailability B\n  with A\n').should.deep.equal(['PLAY1137@1']);
    });

    it('should warn about a condition that reads a value nothing provides', () => {
        codes('module M\n  available when $context.vessel.hasReported == true\n').should.deep.equal(['PLAY1138@2']);
        codes('module M\n  provide vessel String from host\n  available when $context.vessel == "x"\n').should.deep.equal([]);
    });

    it('should report a gate that does not name a kind, a missing name and a name that is not an identifier', () => {
        codes('module M\n  available foo\n').should.deep.equal(['PLAY1132@2']);
        codes('audience A\nmodule M\n  available for A and\n').should.deep.equal(['PLAY1132@3']);
        codes('module M\n  available for ghost\n').should.deep.equal(['PLAY1132@2']);
    });

    it('should report what a declaration body does not take', () => {
        codes('audience A\n  colour red\n').should.deep.equal(['PLAY1130@2']);
        codes('availability V\n  colour red\n').should.deep.equal(['PLAY1131@2']);
    });

    it('should refuse a group nested beyond the supported depth', () => {
        const source = `audience A\nmodule M\n  available for ${'('.repeat(130)}A${')'.repeat(130)}\n`;
        codes(source).should.deep.equal(['PLAY0861@3']);
    });

    it('should locate each persona audience even when one is named twice', () => {
        codes('persona P\n  audience Ghost\n  audience Ghost\n').should.deep.equal(['PLAY1135@2', 'PLAY1135@3']);
    });

    it('should warn about a gate that reads a value on either side that nothing provides', () => {
        codes('module M\n  available when $context.tier == $context.ghost\n').should.deep.equal(['PLAY1138@2', 'PLAY1138@2']);
        codes('module M\n  available when tier == 1\n').should.not.include('PLAY1138@2');
    });

    describe('and a screen invokes a command', () => {
        const invoke = (screenGate: string, commandGate: string, prelude: readonly string[] = []): string[] => codes([
            'audience A', 'audience B', ...prelude, 'module M', '  feature F', '    slice StateChange S',
            '      event E', '        x String', '      command C', `        ${commandGate}`, '        x String', '        produces E', '          x = x',
            '      screen Sc', `        ${screenGate}`, '        action C for screen', ''
        ].join('\n')).filter(code => code.startsWith('PLAY1139'));

        it('should warn when the command can be off while the screen is on', () => {
            invoke('available for A', 'available for B').should.have.length(1);
        });

        it('should not warn when the command is on whenever the screen is', () => {
            invoke('available for A and B', 'available for A').should.deep.equal([]);
        });

        it('should not warn when no audience decides either gate', () => {
            invoke('available when $context.tier == 1', 'available when $context.tier == 2').should.deep.equal([]);
        });

        it('should not warn when more audiences are named than can be compared', () => {
            const names = Array.from({ length: 13 }, (_, index) => `G${index}`);
            const gate = `available for ${names.join(' or ')}`;
            invoke(gate, gate, names.map(name => `audience ${name}`)).should.deep.equal([]);
        });

        it('should leave an availability nothing declares or that builds on itself out of the comparison', () => {
            invoke('available for A', 'available with Ghost').should.deep.equal([]);
            invoke('available for A', 'available with Loop', ['availability Loop', '  with Loop', '  for B']).should.have.length(1);
        });
    });

    it('should never report a persona audience that is declared', () => {
        codes('audience A\npersona P\n  audience A\n').should.deep.equal([]);
        codes('persona P\n  audience Ghost\n').should.deep.equal(['PLAY1135@2']);
    });
});
