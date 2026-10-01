// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { boardHtml } from '../boardHtml';

describe('when rendering the page', () => {
    let html: string;
    let policy: string;

    beforeEach(() => {
        html = boardHtml({ scriptUri: 'https://webview/out/webview.js', styleUri: 'https://webview/out/webview.css', cspSource: 'https://webview', nonce: 'the-nonce' });
        policy = /http-equiv="Content-Security-Policy" content="([^"]*)"/.exec(html)![1];
    });

    it('should run only the script carrying the nonce', () => {
        policy.should.contain('script-src \'nonce-the-nonce\'');
    });

    // The board compiles its shaders without eval (pixi.js/unsafe-eval), so the page never has to allow it.
    it('should not allow eval', () => {
        policy.should.not.contain('unsafe-eval');
    });

    it('should deny anything not listed', () => {
        policy.should.contain('default-src \'none\'');
    });

    it('should load the bundle with the nonce', () => {
        html.should.contain('<script nonce="the-nonce" src="https://webview/out/webview.js"></script>');
    });

    it('should load the stylesheet', () => {
        html.should.contain('<link rel="stylesheet" href="https://webview/out/webview.css">');
    });
});
