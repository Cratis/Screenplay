// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompilationResult, parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

// Parses the lines as one document, so a spec reads the source it is about.
export const a_parsed_document = (...lines: string[]): CompilationResult<ApplicationSyntax> => parse(lines.join('\n'), 'Document.play');
