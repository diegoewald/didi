import type { BackupPayload, EncryptedBackupEnvelope } from '../../types';

const encoder = new TextEncoder();
const decoder = new TextDecoder();
const asBufferSource = (bytes: Uint8Array): ArrayBuffer => bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer;
const iterations = 210_000;

function bytesToBase64(bytes: Uint8Array): string {
  return btoa(String.fromCharCode(...bytes));
}

function base64ToBytes(value: string): Uint8Array {
  return Uint8Array.from(atob(value), (char) => char.charCodeAt(0));
}

async function deriveKey(password: string, salt: Uint8Array): Promise<CryptoKey> {
  const baseKey = await crypto.subtle.importKey('raw', asBufferSource(encoder.encode(password)), 'PBKDF2', false, [
    'deriveKey',
  ]);
  return crypto.subtle.deriveKey(
    { name: 'PBKDF2', salt: asBufferSource(salt), iterations, hash: 'SHA-256' },
    baseKey,
    { name: 'AES-GCM', length: 256 },
    false,
    ['encrypt', 'decrypt'],
  );
}

export async function encryptBackup(
  payload: BackupPayload,
  password: string,
): Promise<EncryptedBackupEnvelope> {
  if (password.length < 8) {
    throw new Error('A senha do backup criptografado deve ter pelo menos 8 caracteres.');
  }
  const salt = crypto.getRandomValues(new Uint8Array(16));
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const key = await deriveKey(password, salt);
  const encrypted = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv: asBufferSource(iv) },
    key,
    asBufferSource(encoder.encode(JSON.stringify(payload))),
  );
  return {
    version: 1,
    encrypted: true,
    exportedAt: payload.exportedAt,
    salt: bytesToBase64(salt),
    iv: bytesToBase64(iv),
    iterations,
    data: bytesToBase64(new Uint8Array(encrypted)),
  };
}

export async function decryptBackup(
  envelope: EncryptedBackupEnvelope,
  password: string,
): Promise<BackupPayload> {
  if (!envelope.encrypted) {
    throw new Error('Arquivo criptografado inválido.');
  }
  const key = await deriveKey(password, base64ToBytes(envelope.salt));
  const decrypted = await crypto.subtle.decrypt(
    { name: 'AES-GCM', iv: asBufferSource(base64ToBytes(envelope.iv)) },
    key,
    asBufferSource(base64ToBytes(envelope.data)),
  );
  return JSON.parse(decoder.decode(decrypted)) as BackupPayload;
}
