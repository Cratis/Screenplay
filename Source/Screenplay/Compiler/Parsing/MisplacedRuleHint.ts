// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from '../Text/patterns';

// Guidance appended to a rejected 'rule <predicate>' line that sits directly in a command or concept body instead
// of inside a 'validate' block. Only a line that reads like a declarative validation predicate gets a hint
// ('rule not empty', 'rule > 0'); a named 'rule <Name>' and every other unknown directive keep the generic
// diagnostic. The hint never says the suggested edit makes the application valid, and the line is still rejected.
const misplacedRule = pattern(String.raw`^rule\s+((?:not\s+empty|(?:not\s+contains|contains|starts\s+with|ends\s+with|matches|length|decimals|all|max|min)\s+\S.*|(?:>=|<=|==|!=|>|<)\s*\S.*))$`);

/** Gets the hint to append to a rejected command body line, or an empty string when it is not a misplaced predicate. */
export function misplacedRuleHintForCommand(content: string): string {
    const rule = misplacedRule.exec(content)?.[1];
    return rule === undefined ? '' : ` - to validate a property, declare 'validate' and put '<property> ${rule}' on an indented line beneath it`;
}

/** Gets the hint to append to a rejected concept body line, or an empty string when it is not a misplaced predicate. */
export function misplacedRuleHintForConcept(content: string): string {
    const rule = misplacedRule.exec(content)?.[1];
    return rule === undefined ? '' : `. To validate the concept's value, declare 'validate' and put '${rule}' on an indented line beneath it`;
}
