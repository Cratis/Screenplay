// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const corpus = resolve(__dirname, '../../../DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1');
const read = (path: string): string => readFileSync(resolve(corpus, path), 'utf8');

// The ui profile root template vectors are the C# compiler's to model; this compiler reads past a ui profile. It has to read every
// vector without trouble and report what it shares with the editors: the legacy layout line.
describe('when parsing the ui profile root corpus vectors', () => {
    it.each(['positive/UiProfileRoot.play.txt', 'negative/UiProfileRootUnknown.play.txt', 'negative/UiProfileRootNotAShell.play.txt', 'negative/UiProfileRootInvalid.play.txt'])('should read %s without a diagnostic', path => {
        expect(parse(read(path)).diagnostics).toEqual([]);
    });

    it('should report the legacy layout line of each profile as PLAY0791 on the profile line', () => {
        const diagnostics = parse(read('positive/UiProfileRootLegacyLayout.play.txt')).diagnostics;
        expect(diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.line])).toEqual([['PLAY0791', 'warning', 6], ['PLAY0791', 'warning', 10]]);
    });
});
