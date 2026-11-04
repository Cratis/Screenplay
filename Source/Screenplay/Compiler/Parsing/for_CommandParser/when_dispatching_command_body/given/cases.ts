// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The command body characterization matrix, shared with the native specification of the same name. Every case is the body of
// 'command C' followed by a sibling 'command Marker', so recovery and the reader position after each directive are pinned. Each
// expectation is the diagnostics (severity, code, line:column, text) in order, then the serialized syntax of command C.
export interface Case {
    readonly name: string;
        readonly source: string;
    readonly expected: string;
}

export const commandBodyCases: readonly Case[] = [
    {
        name: 'plain properties',
        source: '        name String\n        age Int optional\n        id Uuid identifier',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"name","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"age","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":true,"name":"Int"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":true,"name":"id","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Uuid"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'two identifiers',
        source: '        a Uuid identifier\n        b Uuid identifier',
        expected: 'error PLAY0036 13:9 Command \'C\' already marks \'a\' as identifier - only one property can be the identifier\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":true,"name":"a","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Uuid"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"b","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Uuid"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'legacy optional suffix',
        source: '        name String?',
        expected: 'information PLAY0479 12:14 Write \'String optional\' instead of \'String?\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"name","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":true,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns a property declared before it',
        source: '        value String\n        returns value',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns a property declared after it',
        source: '        returns value\n        value String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns naming no property',
        source: '        returns Missing',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Missing"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns String alone',
        source: '        returns String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns escaped',
        source: '        returns @Id',
        expected: 'error PLAY0487 12:17 Response source \'Id\' must reference one direct command property.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"Id"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns escaped to a declared property',
        source: '        Id String\n        returns @Id',
        expected: 'error PLAY0034 12:9 Unexpected \'Id String\' in command body\nerror PLAY0487 13:17 Response source \'Id\' must reference one direct command property.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"Id"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns a lower case concept with no property',
        source: '        returns slug',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"slug"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns a property named returns',
        source: '        returns String\n        returns returns',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"returns"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'two scalar responses',
        source: '        value String\n        returns value\n        returns String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'two unconditional blocks',
        source: '        returns\n          a = b\n        returns\n          c = d',
        expected: 'error PLAY0486 14:9 A command declares at most one unconditional response.\nerror PLAY0487 13:15 Response source \'b\' must reference one direct command property.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":{"kind":"RecordCommandResponseSyntax","fields":[{"kind":"ResponseFieldSyntax","name":"a","source":{"kind":"PropertyResponseSourceSyntax","property":"b"},"type":null}]},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns block',
        source: '        returns\n          a = b',
        expected: 'error PLAY0487 13:15 Response source \'b\' must reference one direct command property.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":{"kind":"RecordCommandResponseSyntax","fields":[{"kind":"ResponseFieldSyntax","name":"a","source":{"kind":"PropertyResponseSourceSyntax","property":"b"},"type":null}]},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'scalar returns with a child',
        source: '        value String\n        returns value\n          child String',
        expected: 'error PLAY0486 14:11 A scalar response cannot have child directives.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns with an invalid modifier order',
        source: '        returns String key optional',
        expected: 'error PLAY0660 12:9 Write \'key\' once, without other modifiers: \'<name> <Type> key\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns with a reversed modifier order',
        source: '        returns String identifier optional',
        expected: 'error PLAY0480 12:9 Write \'optional\' before \'identifier\': \'<name> <Type> optional identifier\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns with a key and another modifier',
        source: '        returns String key generated',
        expected: 'error PLAY0660 12:9 Write \'key\' once, without other modifiers: \'<name> <Type> key\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns with a subject modifier order',
        source: '        returns String subject optional',
        expected: 'error PLAY0595 12:9 Write each modifier once in order: \'<name> <Type> optional generated identifier subject\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns not a scalar',
        source: '        returns a b c d',
        expected: 'error PLAY0486 12:9 Expected \'returns <property>\' or an unconditional \'returns\' block.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns declared twice as a property',
        source: '        returns Int\n        returns Int',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Int"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"returns","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Int"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'returns a property then a block',
        source: '        value String\n        returns value\n        returns\n          a = b',
        expected: 'error PLAY0486 14:9 A command declares at most one unconditional response.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar twice',
        source: '        calendar "Europe/Oslo"\n        calendar "Asia/Tokyo"',
        expected: 'error PLAY0813 13:9 A command declares at most one \'calendar "<id>"\' or \'calendar from $context.<member>\'.\n{"kind":"CommandSyntax","authorize":null,"calendar":{"kind":"CommandCalendarSyntax","contextMember":null,"id":"Europe/Oslo"},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar from the context',
        source: '        calendar from $context.tz',
        expected: '{"kind":"CommandSyntax","authorize":null,"calendar":{"kind":"CommandCalendarSyntax","contextMember":"tz","id":null},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar from a nested member',
        source: '        calendar from $context.a.b',
        expected: '{"kind":"CommandSyntax","authorize":null,"calendar":{"kind":"CommandCalendarSyntax","contextMember":"a.b","id":null},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar malformed',
        source: '        calendar "x',
        expected: 'error PLAY0813 12:9 A command declares at most one \'calendar "<id>"\' or \'calendar from $context.<member>\'.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar as a property',
        source: '        calendar String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"calendar","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'calendar then a property',
        source: '        calendar "Europe/Oslo"\n        when String',
        expected: '{"kind":"CommandSyntax","authorize":null,"calendar":{"kind":"CommandCalendarSyntax","contextMember":null,"id":"Europe/Oslo"},"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"when","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'stream with a candidate',
        source: '        stream Account.Transactions',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Transactions","streamId":null,"streamIdParts":[]},"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'stream without a candidate',
        source: '        stream Missing.Stream',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"stream","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Missing.Stream"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'stream twice',
        source: '        stream Account.Transactions\n        stream Account.Transactions',
        expected: 'error PLAY0504 13:9 A command declares at most one stream route.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Transactions","streamId":null,"streamIdParts":[]},"streamCandidates":[{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Transactions","streamId":null,"streamIdParts":[]}],"validations":[]}'
    },
    {
        name: 'stream with a property of the same type',
        source: '        @stream Account.Transactions optional\n        stream Account.Transactions',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"stream","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":true,"name":"Account.Transactions"}}],"response":null,"stream":{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Transactions","streamId":null,"streamIdParts":[]},"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'stream as a property',
        source: '        stream String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"stream","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'stream with an unknown stream',
        source: '        stream Account.Other',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Other","streamId":null,"streamIdParts":[]},"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'runs misplaced',
        source: '        runs Foo',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"runs","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Foo"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'runs as a property',
        source: '        runs String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"runs","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'authorize',
        source: '        authorize Admin',
        expected: '{"kind":"CommandSyntax","authorize":{"kind":"AuthorizeSyntax","requirement":{"kind":"PolicyReferenceSyntax","name":"Admin"}},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'authorize repeated',
        source: '        authorize Admin\n        authorize Editor',
        expected: '{"kind":"CommandSyntax","authorize":{"kind":"AuthorizeSyntax","requirement":{"kind":"LogicalPolicyRequirementSyntax","left":{"kind":"PolicyReferenceSyntax","name":"Admin"},"operator":"And","right":{"kind":"PolicyReferenceSyntax","name":"Editor"}}},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'authorize repeated three times',
        source: '        authorize Admin\n        authorize Editor\n        authorize Viewer',
        expected: '{"kind":"CommandSyntax","authorize":{"kind":"AuthorizeSyntax","requirement":{"kind":"LogicalPolicyRequirementSyntax","left":{"kind":"LogicalPolicyRequirementSyntax","left":{"kind":"PolicyReferenceSyntax","name":"Admin"},"operator":"And","right":{"kind":"PolicyReferenceSyntax","name":"Editor"}},"operator":"And","right":{"kind":"PolicyReferenceSyntax","name":"Viewer"}}},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'authorize as a property',
        source: '        authorize String',
        expected: '{"kind":"CommandSyntax","authorize":{"kind":"AuthorizeSyntax","requirement":{"kind":"PolicyReferenceSyntax","name":"String"}},"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'description',
        source: '        description "Does a thing"',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":"Does a thing","handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'description twice',
        source: '        description "a"\n        description "b"',
        expected: 'error PLAY0147 13:9 Command \'C\' already declares a description - at most one is allowed\n{"kind":"CommandSyntax","authorize":null,"description":"a","handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'description as a property',
        source: '        description String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"description","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'documentation',
        source: '        documentation "Docs"',
        expected: 'error PLAY0558 12:9 Command \'C\' documentation requires a fenced markdown block\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'documentation twice',
        source: '        documentation "a"\n        documentation "b"',
        expected: 'error PLAY0558 12:9 Command \'C\' documentation requires a fenced markdown block\nerror PLAY0558 13:9 Command \'C\' documentation requires a fenced markdown block\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'documentation as a property',
        source: '        documentation String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"documentation","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'validate a property',
        source: '        validate String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"validate","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'validate csharp',
        source: '        validate csharp',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[{"kind":"CodeValidateSyntax"}]}'
    },
    {
        name: 'validate a rule',
        source: '        name String\n        validate name not empty',
        expected: 'error PLAY0140 13:9 Invalid validate declaration \'validate name not empty\' - expected \'validate\' or \'validate csharp\'\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"name","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'validate malformed',
        source: '        validate',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[{"kind":"DeclarativeValidateSyntax","rules":[]}]}'
    },
    {
        name: 'produces',
        source: '        produces Opened',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[{"kind":"ProducesSyntax","event":"Opened","for":null,"inlineEvent":null,"inlineOperation":null,"mappings":[],"tags":[]}],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'produces with an event body',
        source: '        produces event Opened\n          name String = name',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[{"kind":"ProducesSyntax","event":"Opened","for":null,"inlineEvent":{"kind":"EventSyntax","description":null,"documentation":null,"generation":1,"hasGenerationMarker":false,"id":null,"name":"Opened","properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"name","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"tags":[]},"inlineOperation":null,"mappings":[{"kind":"PropertyMappingSyntax","property":"name","source":{"kind":"PathExpressionSyntax","path":"name"}}],"tags":[]}],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'produces malformed',
        source: '        produces',
        expected: 'error PLAY0042 12:9 Invalid produces declaration \'produces\' - expected \'produces <EventType>\' or \'produces when <condition>\'\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler',
        source: '        handler\n          implementation',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":null,"implementation":{"kind":"ImplementationSyntax","hints":[]}},"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler twice',
        source: '        handler\n          implementation\n          hint "one"\n        handler\n          implementation\n          hint "two"',
        expected: 'error PLAY0494 14:11 A handler has one implementation wrapper and cannot mix wrapped and direct sources.\nerror PLAY0494 17:11 A handler has one implementation wrapper and cannot mix wrapped and direct sources.\nerror PLAY0698 15:9 A pending handler intent cannot coexist with another handler on the same command.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":null,"implementation":{"kind":"ImplementationSyntax","hints":[]}},"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler three times',
        source: '        handler\n          implementation\n        handler\n          implementation\n        handler\n          implementation',
        expected: 'error PLAY0698 14:9 A pending handler intent cannot coexist with another handler on the same command.\nerror PLAY0698 16:9 A pending handler intent cannot coexist with another handler on the same command.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":null,"implementation":{"kind":"ImplementationSyntax","hints":[]}},"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler as a property',
        source: '        handler String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"handler","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler with a file',
        source: '        handler\n          file First.cs\n        handler\n          file Last.cs',
        expected: 'error PLAY0168 14:9 A command cannot declare more than one handler.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":{"kind":"FileReferenceSyntax","path":"Last.cs"},"implementation":null},"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler with an inline code block',
        source: '        handler\n          implementation\n            ```csharp\n            return null;\n            ```\n        handler\n          file Last.cs',
        expected: 'error PLAY0168 17:9 A command cannot declare more than one handler.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":{"kind":"FileReferenceSyntax","path":"Last.cs"},"implementation":null},"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'handler with produces',
        source: '        produces Opened\n        handler\n          implementation',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":{"kind":"HandlerSyntax","code":null,"file":null,"implementation":{"kind":"ImplementationSyntax","hints":[]}},"name":"C","produces":[{"kind":"ProducesSyntax","event":"Opened","for":null,"inlineEvent":null,"inlineOperation":null,"mappings":[],"tags":[]}],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads',
        source: '        reads X',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads with an alias',
        source: '        reads X as x',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads with a key',
        source: '        reads X as x by id',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads optional',
        source: '        reads X optional',
        expected: 'error PLAY0481 12:9 Optional reads are not yet supported (see #308).\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads as a property',
        source: '        reads String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'reads with children',
        source: '        reads X as x\n          note String',
        expected: 'error PLAY0451 13:11 A reads line allows only one by child block, without a single by value.\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'concurrency',
        source: '        concurrency\n          eventSource',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'concurrency twice',
        source: '        concurrency\n          eventSource\n        concurrency',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'concurrency invalid',
        source: '        concurrency extra',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"concurrency","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"extra"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'concurrency as a property',
        source: '        concurrency String',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"concurrency","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'unknown directive',
        source: '        frobnicate',
        expected: 'error PLAY0034 12:9 Unexpected \'frobnicate\' in command body\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'unknown directive with children',
        source: '        frobnicate\n          nested String',
        expected: 'error PLAY0034 12:9 Unexpected \'frobnicate\' in command body\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'unknown directive with a modifier error',
        source: '        frobnicate String key optional',
        expected: 'error PLAY0660 12:9 Write \'key\' once, without other modifiers: \'<name> <Type> key\'.\nerror PLAY0034 12:9 Unexpected \'frobnicate String key optional\' in command body\n{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'misplaced rule hint',
        source: '        require x',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"require","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"x"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'misplaced rule hint on',
        source: '        when x',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"when","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"x"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'misplaced rule hint reject',
        source: '        reject x',
        expected: '{"kind":"CommandSyntax","authorize":null,"description":null,"handler":null,"name":"C","produces":[],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"reject","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"x"}}],"response":null,"stream":null,"streamCandidates":[],"validations":[]}'
    },
    {
        name: 'everything once',
        source: '        description "d"\n        documentation "doc"\n        calendar "Europe/Oslo"\n        id Uuid identifier\n        name String\n        value String\n        returns value\n        stream Account.Transactions\n        authorize Admin\n        validate name not empty\n        reads X as x\n        concurrency\n          eventSource\n        produces Opened\n        handler\n          implementation',
        expected: 'error PLAY0558 13:9 Command \'C\' documentation requires a fenced markdown block\nerror PLAY0140 21:9 Invalid validate declaration \'validate name not empty\' - expected \'validate\' or \'validate csharp\'\nerror PLAY0504 15:9 Command identifier \'id\' does not have the source\'s nominal identifier type \'AccountId\'. The stream does not supply a destination.\ninformation PLAY0478 25:9 Plain \'produces Opened\' omits its destination - use \'for id\' to explicitly select the command\'s identifier.\n{"kind":"CommandSyntax","authorize":{"kind":"AuthorizeSyntax","requirement":{"kind":"PolicyReferenceSyntax","name":"Admin"}},"calendar":{"kind":"CommandCalendarSyntax","contextMember":null,"id":"Europe/Oslo"},"description":"d","handler":{"kind":"HandlerSyntax","code":null,"file":null,"implementation":{"kind":"ImplementationSyntax","hints":[]}},"name":"C","produces":[{"kind":"ProducesSyntax","event":"Opened","for":null,"inlineEvent":null,"inlineOperation":null,"mappings":[],"tags":[]}],"properties":[{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":true,"name":"id","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"Uuid"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"name","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}},{"kind":"PropertySyntax","isGenerated":false,"isIdentifier":false,"name":"value","type":{"kind":"TypeRefSyntax","isCollection":false,"isOptional":false,"name":"String"}}],"response":{"kind":"ScalarCommandResponseSyntax","source":{"kind":"PropertyResponseSourceSyntax","property":"value"}},"stream":{"kind":"CommandStreamSyntax","eventSource":"Account","propertyCandidate":null,"stream":"Transactions","streamId":null,"streamIdParts":[]},"streamCandidates":[],"validations":[]}'
    }
];
