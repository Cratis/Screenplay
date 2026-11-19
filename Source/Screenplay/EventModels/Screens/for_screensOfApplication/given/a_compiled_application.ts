// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse, ApplicationSyntax } from '@cratis/screenplay-compiler';

// The application the lines describe, compiled the way a board host compiles it.
export const application_of = (...lines: string[]): ApplicationSyntax => parse(lines.join('\n'), 'work.play').value;
