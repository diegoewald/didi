import js from '@eslint/js';
import tseslint from 'typescript-eslint';

const browserGlobals = {
  window: 'readonly',
  document: 'readonly',
  crypto: 'readonly',
  File: 'readonly',
  Blob: 'readonly',
  URL: 'readonly',
  alert: 'readonly',
  confirm: 'readonly',
  prompt: 'readonly',
  localStorage: 'readonly',
  indexedDB: 'readonly',
  history: 'readonly',
  navigator: 'readonly',
  matchMedia: 'readonly',
  btoa: 'readonly',
  atob: 'readonly',
  TextEncoder: 'readonly',
  TextDecoder: 'readonly',
  CryptoKey: 'readonly',
  IDBTransactionOptions: 'readonly',
  console: 'readonly',
};

export default tseslint.config(
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      parserOptions: {
        ecmaFeatures: { jsx: true },
      },
      globals: browserGlobals,
    },
  },
  {
    ignores: ['dist', 'node_modules'],
  },
);
