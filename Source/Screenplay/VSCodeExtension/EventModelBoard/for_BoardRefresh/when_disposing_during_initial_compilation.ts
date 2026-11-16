// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { setImmediate as settled } from 'node:timers/promises';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardRefresh } from '../BoardRefresh';
import { a_compilation_rooted_at, document, host, reset, webview } from './given/a_board_host';

vi.mock('vscode', async importOriginal => ({ ...await importOriginal<object>(), ...(await import('./given/a_board_host')).vscode }));
vi.mock('../boardSources', async () => ({ compileForBoard: (await import('./given/a_board_host')).compileForBoard }));
vi.mock('../compileBoard', async () => ({ boardFor: (await import('./given/a_board_host')).boardFor }));

describe('when disposing during initial compilation', () => {
    let refresh: BoardRefresh;

    beforeEach(async () => {
        reset();
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        refresh = new BoardRefresh(document, webview);
        refresh.now();
        refresh.dispose();
        host.compilations[0](a_compilation_rooted_at('/work/app'));
        await settled();
        refresh.soon();
        refresh.now();
        host.changeDocument(document.uri);
        vi.advanceTimersByTime(1000);
    });

    afterEach(() => vi.useRealTimers());

    it('should not watch the application', () => host.watchers.length.should.equal(0));
    it('should not take the application root', () => (refresh.root === undefined).should.be.true);
    it('should not map the board', () => host.mappings.should.equal(0));
    it('should not show the board', () => host.messages.length.should.equal(0));
    it('should not compile again', () => host.compilations.length.should.equal(1));
});
