// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { beforeAll, describe, it } from 'vitest';
import { compileApplication } from '@cratis/screenplay-compiler';
import { EventModelDocument, FeatureDocument, SliceDocument, SliceSpecificationDocument, SpecificationReadModelDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const samples = resolve(__dirname, '../../../../../Samples');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

function sample(name: string): EventModelDocument {
    const folder = join(samples, name);
    const files = new Map(playFiles(folder).map(file => [relative(folder, file).split('\\').join('/'), readFileSync(file, 'utf8')]));
    return toEventModelDocument(compileApplication(files).value!, name);
}

function slicesOf(document: EventModelDocument): SliceDocument[] {
    const slices = (features: FeatureDocument[]): SliceDocument[] => features.flatMap(feature => [...feature.slices, ...slices(feature.subFeatures)]);
    return document.collections.flatMap(collection => collection.modules).flatMap(module => slices(module.features));
}

// A fixture is visible when the board's read model field carries it or the title still names it.
function hiddenFixtures(specification: SliceSpecificationDocument): string[] {
    const hidden = (side: string, fixtures: { name: string }[] | undefined, onCard: SpecificationReadModelDocument | undefined) =>
        Object.entries((fixtures ?? []).reduce<Record<string, number>>((counts, fixture) => ({ ...counts, [fixture.name]: (counts[fixture.name] ?? 0) + 1 }), {}))
            .filter(([name, count]) => count > specification.name.split(`${side} readmodel ${name} `).length - 1 + (onCard?.name === name ? 1 : 0))
            .map(([name]) => `${specification.name}: ${side} readmodel ${name}`);
    return [...hidden('given', specification.givenReadModels, specification.givenReadModel), ...hidden('then', specification.thenReadModels, specification.thenReadModel)];
}

describe('when mapping the read model fixtures of the samples', () => {
    const documents = new Map<string, EventModelDocument>();
    let auditing: SliceSpecificationDocument;

    beforeAll(() => {
        for (const name of readdirSync(samples).filter(entry => statSync(join(samples, entry)).isDirectory())) {
            documents.set(name, sample(name));
        }
        auditing = slice_named(documents.get('Invoicing')!, 'RecordPayment').specifications.find(specification => specification.name.startsWith('Auditing A Cash Payment'))!;
    });

    it('should keep both fixtures of auditing a cash payment in its title', () => {
        auditing.name.should.contain('given readmodel InvoiceBalanceReadModel { invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9", total = 100, paid = 0, outstanding = 100 }');
        auditing.name.should.contain('given readmodel InvoicePaymentMethodTotals { invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9", method = "cash", total = 0 }');
    });
    it('should keep every fixture of every sample on the board or in the title', () =>
        [...documents.values()].flatMap(document => slicesOf(document).flatMap(slice => slice.specifications.flatMap(hiddenFixtures))).should.deep.equal([]));
});
