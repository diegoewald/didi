import { describe, expect, it } from 'vitest';
import { decryptBackup, encryptBackup } from '../lib/export/cryptoBackup';
import { parseBackup } from '../lib/export/backup';
import type { BackupPayload } from '../types';

const payload: BackupPayload = {
  version: 1,
  exportedAt: '2026-05-08T00:00:00.000Z',
  transactions: [],
  categories: [],
  accounts: [],
  creditCards: [],
  budgets: [],
  goals: [],
  settings: { id: 'settings', currency: 'BRL', theme: 'dark', dateFormat: 'dd/MM/yyyy', financialMonthStart: 1, cashView: 'caixa' },
};

describe('backup local', () => {
  it('restaura backup JSON simples validado', async () => {
    const file = new File([JSON.stringify(payload)], 'backup.json', { type: 'application/json' });
    await expect(parseBackup(file)).resolves.toEqual(payload);
  });

  it('criptografa, descriptografa e rejeita senha incorreta', async () => {
    const encrypted = await encryptBackup(payload, 'senha-forte-123');
    await expect(decryptBackup(encrypted, 'senha-forte-123')).resolves.toEqual(payload);
    await expect(decryptBackup(encrypted, 'senha-errada')).rejects.toThrow(/Senha incorreta/);
  });
});
