// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../Structure';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';

const source = readFileSync(new URL('../../Conformance/exact-named-rule-intent.play', import.meta.url), 'utf8');
const rules = (syntax: ApplicationSyntax) => {
    const validation = syntax.modules[0].features[0].slices[0].commands[0].validations[0];
    if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
    return validation.rules;
};

describe('when round tripping exact named-rule intent', () => {
    it('should retain pending and attached wrappers and exact numeric validation through strict transport', () => {
        const parsed = parse(source);
        parsed.diagnostics.should.deep.equal([]);
        const wire = JSON.stringify(toSyntaxJson(parsed.value));
        const restored = decodeExactSyntaxJson(wire) as ApplicationSyntax;
        // Restoration fills native-owned optional members that the TS parser does not model.
        // The modeled validation rules stay identical, and the complete restored root is stable.
        rules(restored).slice(0, 2).map(toSyntaxJson).should.deep.equal(rules(parsed.value).slice(0, 2).map(toSyntaxJson));
        const complete = JSON.stringify(toSyntaxJson(restored));
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(complete))).should.equal(complete);
        const [pending, attached, numeric] = rules(restored);
        pending.implementation!.hints.map(hint => hint.text).should.deep.equal(['Needs team-owned criteria']);
        (pending.file === null).should.equal(true);
        (pending.code === null).should.equal(true);
        attached.implementation!.hints.map(hint => hint.text).should.deep.equal(['Preserve attached criteria']);
        attached.file!.path.should.equal('Rules/Attached.cs');
        attached.severity.should.equal('Warning');
        attached.message!.should.equal('Invalid label');
        numeric.value!.kind.should.equal('LiteralExpressionSyntax');
        if (numeric.value?.kind === 'LiteralExpressionSyntax') JSON.stringify(numeric.value.value).should.equal('{"literalType":"ExactNumber","value":"9007199254740993"}');
    });

    it('should retain rule implementation in Legacy wire without leaking Exact-only members', () => {
        const parsed = parse(source.replace('numbers exact\n', ''));
        const wire = JSON.stringify(toSyntaxJson(parsed.value));
        wire.should.contain('Needs team-owned criteria');
        wire.should.contain('Preserve attached criteria');
        for (const member of ['sourceOptions', 'requirements', 'policies', 'seeds']) wire.should.not.contain(`"${member}":`);
    });

    it.each(['exact', 'legacy'].flatMap(mode => ['wrong wrapper kind', 'blank hint', 'builtin rule wrapper', 'conflicting payloads'].map(name => ({ mode, name }))))('should reject $name in $mode transport', ({ mode, name }) => {
        const syntax = parse(mode === 'exact' ? source : source.replace('numbers exact\n', '')).value;
        const wire = JSON.parse(JSON.stringify(toSyntaxJson(syntax))) as ApplicationSyntax;
        const pending = rules(wire)[0];
        const changed = name === 'wrong wrapper kind' ? { ...pending, implementation: { kind: 'PathExpressionSyntax', path: 'Wrong' } }
            : name === 'blank hint' ? { ...pending, implementation: { ...pending.implementation, hints: [{ kind: 'ImplementationHintSyntax', text: ' ' }] } }
            : name === 'builtin rule wrapper' ? { ...pending, rule: 'NotEmpty' }
            : { ...pending, file: { kind: 'FileReferenceSyntax', path: 'A.cs' }, code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'true' } };
        Object.assign(pending, changed);
        // The strict reader admits only Exact roots; Legacy still shares writer invariants.
        if (mode === 'exact') (() => decodeExactSyntaxJson(JSON.stringify(wire))).should.throw(InvalidSyntaxJson);
        Object.assign(rules(syntax)[0], changed);
        (() => toSyntaxJson(syntax)).should.throw(InvalidSyntaxJson);
    });

    const malformedMembers = [
        { name: 'wrong file kind', changed: { file: { kind: 'PathExpressionSyntax', path: 'Wrong' } } },
        { name: 'array file node', changed: { file: Object.assign([], { kind: 'FileReferenceSyntax', path: 'A.cs' }) } },
        { name: 'array code node', changed: { code: Object.assign([], { kind: 'CodeBlockSyntax', language: 'csharp', code: 'true' }) } },
        { name: 'array implementation node', changed: { implementation: Object.assign([], { kind: 'ImplementationSyntax', hints: [] }) } },
        { name: 'array hint node', changed: { implementation: { kind: 'ImplementationSyntax', hints: [Object.assign([], { kind: 'ImplementationHintSyntax', text: 'Keep' })] } } },
        ...[undefined, null, ['A.cs'], 42, {}].map(path => ({ name: `nonstring file path ${JSON.stringify(path)}`, changed: { file: { kind: 'FileReferenceSyntax', path } } })),
        { name: 'wrong code kind', changed: { code: { kind: 'PathExpressionSyntax', path: 'Wrong' } } },
        ...['language', 'code'].flatMap(member => [undefined, null, ['csharp'], 42, {}].map(value => ({ name: `nonstring inline ${member} ${JSON.stringify(value)}`, changed: { code: { kind: 'CodeBlockSyntax', language: 'csharp', code: 'true', [member]: value } } }))),
        ...[undefined, null, ['Keep'], 42, true, {}].map(text => ({ name: `nonstring hint text ${JSON.stringify(text)}`, changed: { implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text }] } } })),
        ...[undefined, null, 'Keep', {}, [null], [{ kind: 'PathExpressionSyntax', path: 'Wrong' }], new Array(1)].map(hints => ({ name: `invalid hints collection ${JSON.stringify(hints)}`, changed: { implementation: { kind: 'ImplementationSyntax', hints } } })),
        { name: 'nonstring predicate name', changed: { value: { kind: 'PathExpressionSyntax', path: ['Check'] } } }
    ];

    it.each(['exact', 'legacy'].flatMap(mode => malformedMembers.map(test => ({ mode, ...test }))))('should reject wrapped rule with $name in $mode writing', ({ mode, name, changed }) => {
        const syntax = parse(mode === 'exact' ? source : source.replace('numbers exact\n', '')).value;
        const wire = JSON.parse(JSON.stringify(toSyntaxJson(syntax))) as ApplicationSyntax;
        Object.assign(rules(wire)[0], changed);
        // Native restoration defaults an omitted hints member to an empty collection.
        // Typed writing still requires the in-memory wrapper to carry its collection.
        if (mode === 'exact' && name !== 'invalid hints collection undefined') (() => decodeExactSyntaxJson(JSON.stringify(wire))).should.throw(InvalidSyntaxJson);
        Object.assign(rules(syntax)[0], changed);
        (() => toSyntaxJson(syntax)).should.throw(InvalidSyntaxJson);
        (() => toCompleteSyntaxJson(syntax)).should.throw(InvalidSyntaxJson);
    });

    it.each(['exact', 'legacy'].flatMap(mode => ['pending', 'file', 'inline'].map(payload => ({ mode, payload }))))('should retain scalar payloads and authored hint order for $payload rules in $mode transport', ({ mode, payload }) => {
        const syntax = parse(mode === 'exact' ? source : source.replace('numbers exact\n', '')).value;
        const hints = ['  First  ', 'Second', '  First  '].map(text => ({ kind: 'ImplementationHintSyntax', text }));
        Object.assign(rules(syntax)[0], {
            implementation: { kind: 'ImplementationSyntax', hints },
            file: payload === 'file' ? { kind: 'FileReferenceSyntax', path: '' } : null,
            code: payload === 'inline' ? { kind: 'CodeBlockSyntax', language: '', code: '' } : null
        });
        const wire = JSON.parse(JSON.stringify(toSyntaxJson(syntax))) as ApplicationSyntax;
        rules(wire)[0].implementation!.hints.should.deep.equal(hints);
        ({ file: rules(wire)[0].file, code: rules(wire)[0].code }).should.deep.equal({ file: rules(syntax)[0].file, code: rules(syntax)[0].code });
        if (mode === 'exact') rules(decodeExactSyntaxJson(JSON.stringify(wire)) as ApplicationSyntax)[0].implementation!.hints.map(hint => hint.text).should.deep.equal(hints.map(hint => hint.text));
    });

    it.each(['exact', 'legacy'])('should reject concept-owned wrappers in %s writing, including complete internal projection', mode => {
        const syntax = parse(`${mode === 'exact' ? 'numbers exact\n' : ''}concept Label : String\n  validate\n    rule Check\n`).value;
        const validation = syntax.concepts[0].validations![0];
        if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
        Object.assign(validation.rules[0], { implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text: 'Valid hint' }] } });
        (() => toSyntaxJson(syntax)).should.throw(InvalidSyntaxJson);
        (() => toCompleteSyntaxJson(syntax)).should.throw(InvalidSyntaxJson);
    });

    it('should reject concept-owned wrappers in strict Exact restoration', () => {
        const wire = JSON.parse(JSON.stringify(toSyntaxJson(parse('numbers exact\nconcept Label : String\n  validate\n    rule Check\n').value))) as ApplicationSyntax;
        const validation = wire.concepts[0].validations![0];
        if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
        Object.assign(validation.rules[0], { implementation: { kind: 'ImplementationSyntax', hints: [{ kind: 'ImplementationHintSyntax', text: 'Valid hint' }] } });
        (() => decodeExactSyntaxJson(JSON.stringify(wire))).should.throw(InvalidSyntaxJson);
    });
});
