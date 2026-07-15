// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import tsParser from '@typescript-eslint/parser';
import reactlint from 'eslint-plugin-react';
import header from '@tony.ganchev/eslint-plugin-header';
import globals from 'globals';

header.rules.header.meta.schema = false;

export default [
    {
        ignores: [
            '**/dist/**',
            '**/out/**',
            '**/node_modules/**',
            '**/*.d.ts',
            '**/vite.config.ts',
        ],
    },
    eslint.configs.recommended,
    ...tseslint.configs.recommended,
    {
        files: ['**/*.ts', '**/*.tsx'],
        languageOptions: {
            parser: tsParser,
            parserOptions: {
                ecmaVersion: 'latest',
                sourceType: 'module',
                ecmaFeatures: {
                    jsx: true,
                },
            },
            globals: {
                ...globals.browser,
            },
        },
        plugins: {
            react: reactlint,
            header: header,
        },
        settings: {
            react: {
                version: 'detect',
            },
        },
        rules: {
            semi: [2, 'always'],
            'react/react-in-jsx-scope': 0,
            'react/prop-types': 0,
            '@typescript-eslint/no-unused-vars': [
                'error',
                {
                    argsIgnorePattern: '^_',
                    varsIgnorePattern: '^_',
                },
            ],
            'header/header': [
                2,
                'line',
                [
                    ' Copyright (c) Cratis. All rights reserved.',
                    ' Licensed under the MIT license. See LICENSE file in the project root for full license information.',
                ],
                2,
            ],
        },
    },
    {
        files: ['**/index.ts'],
        rules: {
            'header/header': 'off',
        },
    },
    {
        files: ['**/*.mjs'],
        languageOptions: {
            globals: {
                ...globals.node,
            },
        },
    },
];
