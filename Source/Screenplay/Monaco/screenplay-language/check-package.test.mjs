// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { checkRuntimeBudget } from './check-package.mjs';

test('accepts the per-file boundary and excludes sourcemaps from runtime bytes', () => {
    const result = checkRuntimeBudget([
        { path: 'dist/bundles/index.js', size: 500_000 },
        { path: 'dist/bundles/index.js.map', size: 2_000_000 },
    ]);
    assert.equal(result.runtimeBytes, 500_000);
    assert.equal(result.modules.length, 1);
});

test('planted oversized module fails with its published path', () => {
    assert.throws(() => checkRuntimeBudget([
        { path: 'dist/bundles/compiler/oversized.js', size: 500_001 },
    ]), /Monaco runtime file budget exceeded: dist\/bundles\/compiler\/oversized\.js \(500001 bytes; limit 500000\)/);
});

test('checks every published JavaScript file, including files outside bundles', () => {
    assert.throws(() => checkRuntimeBudget([
        { path: 'dist/bundles/index.js', size: 100 },
        { path: 'dist/accidental.js', size: 500_001 },
    ]), /dist\/accidental\.js/);
});

test('preserves the total runtime ceiling even when individual files fit', () => {
    assert.throws(() => checkRuntimeBudget([
        { path: 'dist/bundles/index.js', size: 500_000 },
        { path: 'dist/bundles/compiler/first.js', size: 500_000 },
        { path: 'dist/bundles/compiler/second.js', size: 1 },
    ]), /Monaco runtime bundle budget exceeded: 1000001 bytes/);
});

test('reports modules largest first at the total runtime boundary', () => {
    const result = checkRuntimeBudget([
        { path: 'dist/bundles/small.js', size: 100 },
        { path: 'dist/bundles/large.js', size: 500_000 },
        { path: 'dist/bundles/medium.js', size: 499_900 },
    ]);
    assert.equal(result.runtimeBytes, 1_000_000);
    assert.equal(result.modules[0].path, 'dist/bundles/large.js');
});

test('refuses an empty runtime instead of passing vacuously', () => {
    assert.throws(() => checkRuntimeBudget([{ path: 'dist/index.d.ts', size: 100 }]), /No published JavaScript modules found/);
});
