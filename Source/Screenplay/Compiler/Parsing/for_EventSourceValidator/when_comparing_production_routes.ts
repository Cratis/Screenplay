// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { validateEventSources } from '../EventSourceValidator';
import { ParserContext } from '../ParserContext';
import { LineReader } from '../LineReader';

const composite = `eventsource Account
  identifier String
  stream Monthly
    streamId
      first String
      second String
module M
  feature F
    slice StateChange S
      command C
        id String identifier
        first String
        second String
        stream Account.Monthly
          streamId
            first = first
            second = "second"
        produces event Changed
          stream Account.Monthly
            streamId
              second = "second"
              first = first
`;

describe('when comparing production routes', () => {
    it('should recognize equal composite mappings despite authored ordering', () => {
        const result = parse(composite);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual([DiagnosticCodes.RedundantProductionRoute]);
        expect(result.value.modules[0].features[0].slices[0].commands[0].produces[0].stream!.streamIdParts.map(part => part.property)).toEqual(['second', 'first']);
    });
    it('should not call different literal mappings redundant', () => {
        const result = parse(composite.replace('second = "second"\n              first', 'second = "other"\n              first'));
        expect(result.diagnostics).toEqual([]);
    });
    it('should report a command identifier mismatch once rather than repeat it for an implicit inline destination', () => {
        const result = parse('eventsource Account\n  identifier Uuid\n  stream All\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        stream Account.All\n        produces event Changed');
        const mismatches = result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidCommandStream);
        expect(mismatches).toHaveLength(1);
        expect(mismatches[0].message).toContain('Command identifier');
        expect(mismatches[0].location.line).toBe(8);
    });
    it('should defer destination comparison when an inline production has no identifier to infer', () => {
        const result = parse('eventsource Account\n  identifier Uuid\n  stream All\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        stream Account.All\n        produces event Changed\n          value String = value');
        expect(result.diagnostics).toEqual([]);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.properties.every(property => !property.isIdentifier)).toBe(true);
        expect(command.produces[0].for).toBeNull();
        expect(command.stream!.stream).toBe('All');
    });
    it.each(['false', '1.5'])('should refuse unsupported scalar stream id %s', value => {
        const exact = value === '1.5' ? 'numbers exact\nconcept Period : Int\n' : '';
        const result = parse(exact + `eventsource Account\n  identifier String\n  stream Monthly\n    streamId ${value === '1.5' ? 'Period' : 'String'}\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        stream Account.Monthly\n          streamId = ${value}\n        produces event Changed`);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.InvalidCommandStream);
        expect(result.value.modules[0].features[0].slices[0].commands[0].stream!.streamId).not.toBeNull();
    });
    it('should not treat a retained property candidate as an authoritative route during draft validation', () => {
        const parsed = parse('import Account.All\ntype All\n  value String\neventsource Account\n  stream All\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.All').value;
        const command = parsed.modules[0].features[0].slices[0].commands[0];
        expect(command.streamCandidates![0].propertyCandidate).not.toBeNull();
        const application = { ...parsed, modules: parsed.modules.map(module => ({ ...module, features: module.features.map(feature => ({ ...feature, slices: feature.slices.map(slice => ({ ...slice, commands: [{ ...command, stream: command.streamCandidates![0], streamCandidates: [] }] })) })) })) };
        const context = new ParserContext(new LineReader([]));
        validateEventSources(application, context);
        expect(context.diagnostics).toEqual([]);
    });
});
