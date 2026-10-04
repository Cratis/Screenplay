// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        ';
const source = '\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\nconcept AccountId : Uuid\nconcept Month : String\n';
const command = (text: string) => parse(text).value.modules[0].features[0].slices[0].commands[0];

describe('when classifying command streams against the whole input', () => {
    it.each(['stream String', 'stream Account.Transactions optional', 'stream Account.Transactions[]', 'stream Account.Transactions generated identifier', '@stream Account.Transactions', 'identifier String', 'eventsource String', 'from String', 'streamId String'])('should retain property form %s', body => {
        const parsed = parse(prefix + body + source);
        expect(parsed.success).toBe(true);
        expect(command(prefix + body + source).stream).toBeNull();
        expect(command(prefix + body + source).properties).toHaveLength(1);
    });
    it.each(['stream Missing.Transactions\n          deeper String', '@stream Account.Transactions\n          deeper String'])('should retain deeper legacy members %s', body => {
        const parsed = parse(prefix + body + source);
        expect(parsed.success).toBe(true);
        expect(command(prefix + body + source).properties.map(property => property.name)).toEqual(['stream', 'deeper']);
        expect(command(prefix + body + source).stream).toBeNull();
    });
    it('should resolve a forward source and its nested mapping', () => {
        const parsed = parse(prefix + 'month Month\n        stream Account.Transactions\n          streamId = month' + source);
        expect(parsed.success).toBe(true);
        const node = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(node.stream?.streamId?.property).toBe('streamId');
        expect(node.properties.map(property => property.name)).toEqual(['month']);
    });
    it('should retain both viable interpretations', () => {
        const parsed = parse('import Account.Transactions\ntype Transactions\n  value String\n' + prefix + 'stream Account.Transactions' + source);
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
        const node = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(node.properties[0].type.name).toBe('Account.Transactions');
        expect(node.stream?.propertyCandidate).not.toBeNull();
    });
    it('should retain a real imported qualified type without a source', () => {
        const node = command('import Account.Transactions\ntype Transactions\n  value String\n' + prefix + 'stream Account.Transactions\n          deeper String');
        expect(node.stream).toBeNull();
        expect(node.properties).toHaveLength(2);
    });
});
