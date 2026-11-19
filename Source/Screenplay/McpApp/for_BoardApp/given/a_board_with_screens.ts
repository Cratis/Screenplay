// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A slice whose screen the bridge can draw, and one whose design lives in a file it cannot read.
export const screenSource = [
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        title "Orders"',
].join('\n');

export const screenInAFileSource = [
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        file "orders.screen.json"',
].join('\n');
