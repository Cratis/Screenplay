// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, it, vi } from 'vitest';
import { should } from 'chai';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairSourceSnapshot } from '../RepairSourceSnapshot';
import { removeSpecFixtureDirectory, specFixtureDirectory } from '../tests/specFixtureDirectory';

vi.mock('node:fs', { spy: true });
should();
let root: string;
const original = Buffer.from('\uFEFF// 😀\r\ncommand Before\r\n');
const entry = (beforePath: string | null = 'A.play', afterPath: string | null = beforePath) => ({ beforePath, afterPath, before: beforePath === null ? null : original, after: afterPath === null ? null : Buffer.from('candidate') });
const file = (name = 'A.play') => path.join(root, name);
beforeEach(() => { root = specFixtureDirectory('repair-source-'); fs.writeFileSync(file(), original); });
afterEach(() => { vi.mocked(fs.openSync).mockReset(); removeSpecFixtureDirectory(root); });

it('should accept exact raw bytes including BOM CRLF and non-BMP text', () => {
    (() => new RepairSourceSnapshot([entry()]).check(root)).should.not.throw();
});
it('should retain hashes independently of mutable review buffers', () => {
    const before = Buffer.from(original);
    const snapshot = new RepairSourceSnapshot([{ ...entry(), before }]);
    before.fill(32); fs.writeFileSync(file(), before);
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
for (const change of ['same-length', 'missing', 'renamed', 'directory']) it(`should refuse a ${change} original without watcher delivery`, () => {
    const snapshot = new RepairSourceSnapshot([entry()]);
    if (change === 'same-length') fs.writeFileSync(file(), Buffer.alloc(original.length, 32));
    else if (change === 'renamed') fs.renameSync(file(), file('moved.play'));
    else { fs.unlinkSync(file()); if (change === 'directory') fs.mkdirSync(file()); }
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should refuse a length mismatch without opening or reading the file', () => {
    const snapshot = new RepairSourceSnapshot([entry()]);
    fs.appendFileSync(file(), 'growth');
    const open = vi.mocked(fs.openSync).mockClear();
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
    open.mock.calls.should.have.length(0);
});
it('should refuse an unreadable original', () => {
    const snapshot = new RepairSourceSnapshot([entry()]);
    vi.mocked(fs.openSync).mockImplementation(() => { throw Object.assign(new Error('denied'), { code: 'EACCES' }); });
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should distinguish an empty original from an absent preimage', () => {
    fs.writeFileSync(file(), '');
    const snapshot = new RepairSourceSnapshot([{ ...entry(), before: Buffer.alloc(0) }]);
    snapshot.check(root);
    fs.unlinkSync(file());
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should require a new destination to remain absent', () => {
    const snapshot = new RepairSourceSnapshot([entry(null, 'new.play')]);
    snapshot.check(root);
    fs.writeFileSync(file('new.play'), 'unexpected');
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should check original bytes for a deletion', () => {
    const snapshot = new RepairSourceSnapshot([entry('A.play', null)]);
    snapshot.check(root);
    fs.unlinkSync(file());
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should check both original bytes and destination absence for a move', () => {
    const snapshot = new RepairSourceSnapshot([entry('A.play', 'new.play')]);
    snapshot.check(root);
    fs.writeFileSync(file('new.play'), original);
    (() => snapshot.check(root)).should.throw().with.property('kind', 'StaleSource');
});
it('should not require another entry original to be absent', () => {
    fs.writeFileSync(file('B.play'), original);
    (() => new RepairSourceSnapshot([entry('A.play', 'B.play'), entry('B.play', 'A.play')]).check(root)).should.not.throw();
});
it('should accept a case-only move when destination and source are the same physical file', () => {
    fs.renameSync(file(), file('a.play'));
    // Case-sensitive hosts use the same native identity without assuming case-insensitivity.
    if (!fs.existsSync(file())) fs.linkSync(file('a.play'), file());
    (() => new RepairSourceSnapshot([entry('A.play', 'a.play')]).check(root)).should.not.throw();
});
for (const unsafe of ['../escape.play', '/absolute.play', 'a\\b.play', 'C:drive.play', 'a//b.play', './A.play', '']) it(`should refuse unsafe path ${JSON.stringify(unsafe)}`, () => {
    (() => new RepairSourceSnapshot([entry(unsafe)])).should.throw().with.property('kind', 'MalformedContract');
});
it('should reject inconsistent path and byte metadata', () => {
    (() => new RepairSourceSnapshot([{ ...entry(), beforePath: null }])).should.throw().with.property('kind', 'MalformedContract');
    (() => new RepairSourceSnapshot([entry(null, null)])).should.throw().with.property('kind', 'MalformedContract');
});
it.skipIf(process.platform === 'win32')('should refuse linked files and linked ancestors even inside the root', () => {
    fs.symlinkSync(file(), file('alias.play'));
    (() => new RepairSourceSnapshot([entry('alias.play')]).check(root)).should.throw().with.property('kind', 'StaleSource');
    fs.symlinkSync(root, file('linked'));
    (() => new RepairSourceSnapshot([entry('linked/A.play')]).check(root)).should.throw().with.property('kind', 'StaleSource');
    (() => new RepairSourceSnapshot([entry(null, 'linked/new.play')]).check(root)).should.throw().with.property('kind', 'StaleSource');
});
