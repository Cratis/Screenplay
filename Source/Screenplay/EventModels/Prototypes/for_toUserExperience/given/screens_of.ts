// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse, ScreenSyntax } from '@cratis/screenplay-compiler';
import { PrototypeElementDocument } from '../../../Document/EventModelDocument';

// The screens of a one-slice document whose slice body is the given lines, indented under the slice.
export function screens_of(...lines: string[]): readonly ScreenSyntax[] {
    const source = ['module M', '  feature F', '    slice StateView S', ...lines.map(line => `      ${line}`)].join('\n');
    return parse(source).value.modules[0].features[0].slices[0].screens;
}

// An element as where and how it is drawn - which is what a layout is about.
export const drawn = (element: PrototypeElementDocument) =>
    [element.type, element.name, element.properties.canvas.x, element.properties.canvas.y, element.width, element.height];
