// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const repository = resolve(__dirname, '../../../..');
const corpus = join(repository, 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus');
const generatedInputSource = join(corpus, 'RegisterProject/v7/rejected/generated-input.play');
const eventRoutes = join(corpus, 'EventRoutes');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        if (entry === 'node_modules' || entry === 'bin' || entry === 'obj') {
            return [];
        }
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

// Scan rejection fixtures too: semantic refusals may parse successfully, while deliberately invalid
// authoring sources pin their exact diagnostics. EventRoutes modules also need their sibling source
// declarations; this single-file scan pins their isolated-fragment diagnostics, not folder admission.
describe('when parsing the repository samples', () => {
    let files: string[];
    let errors: string[];

    beforeAll(() => {
        files = playFiles(join(repository, 'Source'));
        errors = files.flatMap(file => parse(readFileSync(file, 'utf8'), file).diagnostics
            .filter(diagnostic => diagnostic.severity === 'error')
            .map(diagnostic => `${file}:${diagnostic.location.line} ${diagnostic.code} ${diagnostic.message}`));
    });

    it('should find the samples', () => {
        files.length.should.be.greaterThan(20);
    });

    it('should report only the expected rejection and isolated-fragment diagnostics', () => {
        errors.should.deep.equal([
            `${join(eventRoutes, 'composite-commands/source/module.play')}:9 PLAY0034 Unexpected 'streamId' in command body`,
            `${join(eventRoutes, 'composites/source/module.play')}:9 PLAY0034 Unexpected 'streamId' in command body`,
            ...[25, 35, 43, 57].map(line => `${join(eventRoutes, 'composites/source/module.play')}:${line} PLAY0549 Stream 'Account.Ledger' is NotFound; routing requires one physical source and stream.`),
            `${join(eventRoutes, 'observer-filters/source/module.play')}:10 PLAY0034 Unexpected 'streamId = "period"' in command body`,
            ...[21, 25].map(line => `${join(eventRoutes, 'observer-filters/source/module.play')}:${line} PLAY0638 An observer filter must name one physical event source and, when supplied, one stream belonging to it.`),
            `${join(eventRoutes, 'observer-filters/source/module.play')}:35 PLAY0549 Stream 'Account.Notes' is NotFound; routing requires one physical source and stream.`,
            `${join(eventRoutes, 'observer-filters/source/module.play')}:50 PLAY0549 Stream 'Account.All' is NotFound; routing requires one physical source and stream.`,
            `${join(eventRoutes, 'observer-filters/source/module.play')}:55 PLAY0549 Stream 'Account.All' is NotFound; routing requires one physical source and stream.`,
            `${join(eventRoutes, 'production-routes/source/module.play')}:9 PLAY0034 Unexpected 'streamId = period' in command body`,
            ...[[11, 'Notes'], [14, 'Periods'], [22, 'All'], [32, 'Notes']].map(([line, stream]) => `${join(eventRoutes, 'production-routes/source/module.play')}:${line} PLAY0504 Stream 'Account.${stream}' is NotFound; routing requires one physical source and stream.`),
            ...[[40, 'Notes'], [43, 'Periods'], [48, 'Base'], [54, 'All']].map(([line, stream]) => `${join(eventRoutes, 'production-routes/source/module.play')}:${line} PLAY0549 Stream 'Account.${stream}' is NotFound; routing requires one physical source and stream.`),
            `${join(eventRoutes, 'rejections/nfc.play')}:9 PLAY0504 A stream id text literal must be Unicode NFC.`,
            `${join(eventRoutes, 'rejections/production-outside-command.play')}:12 PLAY0636 Only command event productions can declare a stream route.`,
            `${join(eventRoutes, 'rejections/type.play')}:8 PLAY0504 Command identifier 'id' does not have the source's nominal identifier type 'Uuid'. The stream does not supply a destination.`,
            ...[
                [8, 'streamId = key'], [14, 'streamId = 9007199254740991'], [21, 'streamId = 9007199254740990'],
                [28, 'streamId = -9007199254740991'], [35, 'streamId = -9007199254740990'],
                [43, 'streamId = text'], [51, 'streamId = key'], [68, 'streamId = text']
            ].map(([line, text]) => `${join(eventRoutes, 'scalar/source/module.play')}:${line} PLAY0034 Unexpected '${text}' in command body`),
            ...[41, 46].map(line => `${join(eventRoutes, 'specifications/source/module.play')}:${line} PLAY0549 Stream 'Account.All' is NotFound; routing requires one physical source and stream.`),
            `${join(eventRoutes, 'specifications/source/module.play')}:50 PLAY0549 Stream 'Account.Notes' is NotFound; routing requires one physical source and stream.`,
            `${generatedInputSource}:10 PLAY0485 Generated property 'projectId' cannot be supplied as request or form input.`,
            // The metadata-only fixture deliberately declares no operational public output.
            `${join(repository, 'Source/Screenplay/Compiler/Conformance/public-events.play')}:6 PLAY0609 Outbound Translate slice 'PublishInvoice' must produce exactly one local public event type; found 0.`
        ]);
    });
});
