import js from '@eslint/js';
import { defineConfig } from 'eslint/config';
import importPlugin from 'eslint-plugin-import';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default defineConfig([
	{
		files: ['**/*.js'],
		rules: {
			'no-restricted-syntax': [
				'error',
				{
					selector: 'Program',
					message: 'JS/JSX files are not allowed. Use .ts instead.',
				},
			],
		},
	},
	{
		files: ['**/*.ts'],
		extends: [
			js.configs.recommended,
			...tseslint.configs.recommended,
		],
		plugins: {
			import: importPlugin,
		},
		languageOptions: {
			parser: tseslint.parser,
			ecmaVersion: 2020,
			sourceType: 'module',
			globals: globals.browser,
		},
		rules: {
			'import/order': [
				'error',
				{
					groups: [
						'builtin',
						'external',
						['parent', 'sibling', 'index'],
					],
					'newlines-between': 'always',
					alphabetize: { order: 'asc', caseInsensitive: true },
				},
			],

			'no-unused-vars': 'off',
			'@typescript-eslint/no-unused-vars': ['error', {
				args: 'none',
				caughtErrors: 'all',
				caughtErrorsIgnorePattern: '^ignore',
			}],

			'no-redeclare': 'off',
			'@typescript-eslint/no-redeclare': ['error'],

			'no-undef': 'off',

			'no-trailing-spaces': ['error'],
			'brace-style': ['error', '1tbs'],
			'linebreak-style': ['error', 'unix'],
			'operator-linebreak': ['error', 'before'],
			'space-before-function-paren': ['error', 'always'],
			'space-infix-ops': ['error'],
			'object-curly-spacing': ['error', 'always'],
			'quote-props': ['error', 'as-needed'],
			'padded-blocks': ['error', 'never'],
			'space-before-blocks': ['error', 'always'],
			semi: ['error', 'always'],

			'comma-spacing': ['error', { before: false, after: true }],

			'no-multiple-empty-lines': ['error', {
				max: 1,
				maxEOF: 0,
				maxBOF: 0,
			}],

			indent: ['error', 'tab', {
				SwitchCase: 1,
				ignoredNodes: ['TemplateLiteral > *'],
			}],

			quotes: ['error', 'single', {
				avoidEscape: true,
				allowTemplateLiterals: true,
			}],

			'semi-spacing': ['error', { before: false, after: true }],

			'max-len': ['error', { code: 180 }],

			'keyword-spacing': ['error', { before: true, after: true }],

			'comma-dangle': ['error', {
				arrays: 'always-multiline',
				objects: 'always-multiline',
				imports: 'never',
				exports: 'never',
				functions: 'never',
			}],

			'key-spacing': ['error', {
				beforeColon: false,
				afterColon: true,
			}],

			'no-empty': ['off'],
			'no-nested-ternary': ['error'],
			'no-case-declarations': ['off'],
			'no-unreachable': ['warn'],
			'no-ex-assign': ['off'],
			'no-control-regex': ['off'],
			'no-prototype-builtins': ['off'],
			'no-self-assign': ['off'],
			'no-useless-catch': ['off'],
			'no-func-assign': ['off'],
			'no-inner-declarations': ['off'],
			'no-extra-parens': ['off'],
		},
	},
]);