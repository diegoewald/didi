import { describe, expect, it, vi } from 'vitest';
import { createId } from '../lib/utils/id';

describe('createId', () => {
  it('usa crypto.randomUUID quando disponível', () => {
    const randomUUID = vi.spyOn(globalThis.crypto, 'randomUUID').mockReturnValue('uuid-nativo');
    expect(createId()).toBe('uuid-nativo');
    randomUUID.mockRestore();
  });

  it('usa getRandomValues quando randomUUID não existe', () => {
    const originalCrypto = globalThis.crypto;
    Object.defineProperty(globalThis, 'crypto', {
      configurable: true,
      value: {
        getRandomValues(bytes: Uint8Array) {
          bytes.fill(10);
          return bytes;
        },
      },
    });

    expect(createId()).toMatch(/^[a-f0-9]{8}-[a-f0-9]{4}-4[a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$/);

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });

  it('usa fallback final quando Web Crypto não existe', () => {
    const originalCrypto = globalThis.crypto;
    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: undefined });

    expect(createId()).toMatch(/^fp-[a-z0-9]+-[a-z0-9]+$/);

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });
});
