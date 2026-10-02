// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, FeatureSyntax, parseFolder } from '@cratis/screenplay-compiler';
import { narrowTo, scopeOf } from '../../boardScope';

// A folder application, given as its files, narrowed to what one of them stands for. The slices are named as
// 'Module.Feature.Slice', nested features dotted in between.
export function slicesShownFor(path: string, files: Record<string, string[]>): string[] | undefined {
    const documents = new Map(Object.entries(files).map(([file, lines]) => [file, lines.join('\n')]));
    const application = parseFolder([...documents].map(([file, source]) => ({ path: file, source })));
    const narrowed = narrowTo(application.value, scopeOf(path, documents, application));
    return narrowed === undefined ? undefined : sliceNames(narrowed);
}

function sliceNames(application: ApplicationSyntax): string[] {
    const inFeature = (feature: FeatureSyntax, prefix: string): string[] => [
        ...feature.slices.map(slice => `${prefix}.${feature.name}.${slice.name}`),
        ...feature.features.flatMap(nested => inFeature(nested, `${prefix}.${feature.name}`)),
    ];
    return application.modules.flatMap(module => module.features.flatMap(feature => inFeature(feature, module.name)));
}

const slice = (name: string, event: string) => [
    `slice StateChange ${name}`,
    `  event ${event}`,
    '    name String',
];

const indented = (lines: string[], depth: number) => lines.map(line => `${' '.repeat(depth * 2)}${line}`);

// One module with two features, written the way an application composes from imports: the root imports the
// module, the module declares its features and imports their folders, and each slice file holds one slice.
export const composedWithImports: Record<string, string[]> = {
    'application.play': ['domain Acme', 'import "Shared/*.play"', 'import "Ordering/Ordering.play"'],
    'Shared/types.play': ['concept OrderId : Guid'],
    'Ordering/Ordering.play': [
        'module Ordering',
        '  feature Orders',
        '    import "Orders/*.play"',
        '  feature Returns',
        '    import "Returns/*.play"',
    ],
    'Ordering/Orders/PlaceOrder.play': slice('PlaceOrder', 'OrderPlaced'),
    'Ordering/Orders/CancelOrder.play': slice('CancelOrder', 'OrderCancelled'),
    'Ordering/Returns/ReturnOrder.play': slice('ReturnOrder', 'OrderReturned'),
};

// The same application with no imports at all: every file is merged as it is, so each slice file restates the
// module and the feature it belongs to, and the module file only declares them.
export const mergedByFolder: Record<string, string[]> = {
    'application.play': ['domain Acme'],
    'Shared/types.play': ['concept OrderId : Guid'],
    'Ordering/Ordering.play': ['module Ordering', '  feature Orders', '  feature Returns'],
    'Ordering/Orders/PlaceOrder.play': ['module Ordering', '  feature Orders', ...indented(slice('PlaceOrder', 'OrderPlaced'), 2)],
    'Ordering/Orders/CancelOrder.play': ['module Ordering', '  feature Orders', ...indented(slice('CancelOrder', 'OrderCancelled'), 2)],
    'Ordering/Returns/ReturnOrder.play': ['module Ordering', '  feature Returns', ...indented(slice('ReturnOrder', 'OrderReturned'), 2)],
};
