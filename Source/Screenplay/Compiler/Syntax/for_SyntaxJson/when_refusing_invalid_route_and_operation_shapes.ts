// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { CommandStreamSyntax } from '../CommandStreamSyntax';
import { EventSourceSyntax } from '../EventSourceSyntax';
import { TypeRefSyntax } from '../Declarations';
import { validateSyntaxInvariants } from '../SyntaxInvariants';

const prefix = 'eventsource Account\n  identifier String\n  stream All\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        produces event Changed\n          value String = value';
const location = { line: 1, column: 1 };

describe('when refusing invalid route and operation shapes', () => {
    it('should refuse a production route containing an ambiguous property candidate', () => {
        const command = parse(prefix).value.modules[0].features[0].slices[0].commands[0];
        const stream: CommandStreamSyntax = { kind: 'CommandStreamSyntax', eventSource: 'Account', stream: 'All', streamId: null, streamIdParts: [], propertyCandidate: command.properties[0], location };
        expect(() => validateSyntaxInvariants({ ...command.produces[0], stream })).toThrow('A production route cannot contain a property candidate.');
    });
    it('should refuse the wrong node kind in an authoritative command stream', () => {
        const command = parse(prefix).value.modules[0].features[0].slices[0].commands[0];
        const stream = { kind: 'ObserverFilterSyntax', eventSource: 'Account', stream: 'All', location } as unknown as CommandStreamSyntax;
        expect(() => validateSyntaxInvariants({ ...command, stream })).toThrow('The authoritative command stream must be a command stream node.');
    });
    it('should refuse a source identifier that is not a type reference', () => {
        const source = parse(prefix).value.eventSources![0];
        const identifier = { kind: 'PropertySyntax', location } as unknown as TypeRefSyntax;
        const invalid: EventSourceSyntax = { ...source, identifier };
        expect(() => validateSyntaxInvariants(invalid)).toThrow('Source identifiers and stream ids require type reference nodes.');
    });
    it('should refuse operation mappings reordered independently of declared inputs', () => {
        const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        produces operation Send\n          uses Mailer\n          first String = value\n          second String = value';
        const production = parse(source).value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(production.inlineOperation!.inputs.map(input => input.name)).toEqual(['first', 'second']);
        expect(() => validateSyntaxInvariants({ ...production, mappings: [...production.mappings].reverse() })).toThrow('Inline operation inputs and mappings must correspond in order.');
    });
});
