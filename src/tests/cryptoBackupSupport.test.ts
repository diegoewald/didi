import { describe, expect, it } from 'vitest';
import { decryptBackup, encryptedBackupUnavailableMessage } from '../lib/export/cryptoBackup';

describe('backup criptografado em contexto incompatível', () => {
  it('retorna erro amigável quando crypto.subtle não existe', async () => {
    const originalCrypto = globalThis.crypto;
    Object.defineProperty(globalThis, 'crypto', {
      configurable: true,
      value: { getRandomValues: originalCrypto.getRandomValues.bind(originalCrypto) },
    });

    await expect(
      decryptBackup({ version: 1, encrypted: true, exportedAt: '', salt: '', iv: '', iterations: 1, data: '' }, 'senha-forte'),
    ).rejects.toThrow(encryptedBackupUnavailableMessage);

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });
});
