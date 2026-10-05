// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { it, expect, vi } from 'vitest';
import { RepairConnectionOwner } from '../RepairConnectionOwner';
import { RepairSession } from '../RepairSession';

function owner(dispatched = false, inFlight = dispatched) {
    const value = new RepairConnectionOwner(7);
    value.session = { applyDispatched: dispatched, applyInFlight: inFlight, invalidate: vi.fn(), dispose: vi.fn(), trace: vi.fn() } as unknown as RepairSession;
    value.resources.push({ dispose: vi.fn() });
    return value;
}
it('retires undispatched authority and all watchers once; late owners cannot publish into a replacement', () => {
    const old = owner();
    const resource = old.resources[0];
    old.retire(); old.retire();
    expect(resource.dispose).toHaveBeenCalledTimes(1);
    expect(old.session.dispose).toHaveBeenCalledTimes(1);
    expect(old.resources).toHaveLength(0);
    expect(() => old.assertCurrent(owner(), 8)).toThrow('Root or configuration changed');
});
it('invalidates review but retains the dispatched process until its outcome is classified', () => {
    const old = owner(true);
    old.retire();
    expect(old.session.invalidate).toHaveBeenCalledTimes(1);
    expect(old.session.dispose).not.toHaveBeenCalled();
    expect(() => old.assertCurrent(old, 7)).toThrow('Root or configuration changed');
});
it('disposes a connection whose uncertain Apply has already settled, while keeping the dispatched record', () => {
    const settled = owner(true, false);
    settled.retire('root-configuration');
    expect(settled.session.invalidate).toHaveBeenCalledTimes(1);
    expect(settled.session.dispose).toHaveBeenCalledTimes(1);
});
it('checks both owner identity and generation before any async publication', () => {
    const value = owner();
    expect(() => value.assertCurrent(value, 7)).not.toThrow();
    expect(() => value.assertCurrent(value, 8)).toThrow();
    expect(() => value.assertCurrent(undefined, 7)).toThrow();
});
