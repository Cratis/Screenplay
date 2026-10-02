// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from '../Text/patterns';
import { firstWord } from './LineText';
import { SourceLine } from './SourceLine';

const typeReferencePattern = pattern('^[A-Za-z_]\\w*(?:\\[\\])?(?:\\?|\\s+optional)?$');

const pathOf = (line: SourceLine): string =>
    firstWord(line.content) === 'file' ? line.content.substring('file'.length).trim() : '';

// A 'file <path>' directive. This compiler does not model file references; it only recognizes the line so
// it is not misread as something else.
export const isFileDirective = (line: SourceLine): boolean => pathOf(line).length > 0;

// Among properties 'file Path' is a property named file, so only a path that is not type-shaped counts.
export const isFileDirectiveAmongProperties = (line: SourceLine): boolean =>
    isFileDirective(line) && !typeReferencePattern.test(pathOf(line));
