// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const emptyGuid = '00000000-0000-0000-0000-000000000000';

// A GUID derived from a name, so the same element of a document gets the same id every time it is
// compiled. The board keys what it remembers - what is collapsed, what is selected - by id, and a fresh id
// on every keystroke would reset all of it. It is a 128-bit FNV-1a hash in the shape of a version 8 GUID:
// stable and well spread, not cryptographic.
export function guidFor(name: string): string {
    let hash = 0x6c62272e07bb014262b821756295c58dn;
    const prime = 0x0000000001000000000000000000013bn;
    const mask = (1n << 128n) - 1n;
    // Each UTF-16 code unit contributes both of its bytes, so the hash needs no text encoder and runs the
    // same in a webview, in Node and in a browser.
    for (let index = 0; index < name.length; index++) {
        const unit = name.charCodeAt(index);
        for (const byte of [unit >> 8, unit & 0xff]) {
            hash = ((hash ^ BigInt(byte)) * prime) & mask;
        }
    }
    const hex = hash.toString(16).padStart(32, '0').split('');
    hex[12] = '8';
    hex[16] = ((parseInt(hex[16], 16) & 0x3) | 0x8).toString(16);
    const text = hex.join('');
    return `${text.substring(0, 8)}-${text.substring(8, 12)}-${text.substring(12, 16)}-${text.substring(16, 20)}-${text.substring(20)}`;
}
