// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DesignAssignmentSyntax } from '../Syntax/DesignSystemSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { SourceLine, locationOf } from './SourceLine';

const assignmentPattern = new RegExp(`^(?<name>[A-Za-z_][\\w.\\-]*)\\s*=\\s*(?:"(?<literal>${stringBodyPattern})"|(?<reference>[A-Za-z_][\\w.\\-]*))$`);

// Reads `<name> = "<literal>"` or `<name> = <reference>` - the one line shape a theme, a component and the words of a design
// system assign values with. Undefined when the line is neither.
export function parseDesignAssignment(line: SourceLine): DesignAssignmentSyntax | undefined {
    const groups = assignmentPattern.exec(line.content)?.groups;
    if (groups === undefined) return undefined;
    const name = groups.name;
    return groups.literal !== undefined
        ? { kind: 'DesignAssignmentSyntax', name, value: unescapeString(groups.literal), isLiteral: true, location: locationOf(line) }
        : { kind: 'DesignAssignmentSyntax', name, value: groups.reference, isLiteral: false, location: locationOf(line) };
}
