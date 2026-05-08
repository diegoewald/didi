import type { BackupPayload, EncryptedBackupEnvelope } from '../../types';

const encoder = new TextEncoder();
const decoder = new TextDecoder();
const iterations = 210_000;
const base64ChunkSize = 0x8000;

function stableBytes(bytes: Uint8Array): Uint8Array<ArrayBuffer> {
  const copy = new Uint8Array(bytes.byteLength);
  copy.set(bytes);
  return copy as Uint8Array<ArrayBuffer>;
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = '';
  for (let index = 0; index < bytes.length; index += base64ChunkSize) {
    const chunk = bytes.subarray(index, index + base64ChunkSize);
    binary += String.fromCharCode.apply(null, Array.from(chunk));
  }
  return btoa(binary);
}

function base64ToBytes(value: string): Uint8Array {
  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }
  return bytes;
}

async function deriveKey(password: string, salt: Uint8Array): Promise<CryptoKey> {
  const baseKey = await crypto.subtle.importKey('raw', stableBytes(encoder.encode(password)), 'PBKDF2', false, [
    'deriveKey',
  ]);
  return crypto.subtle.deriveKey(
    { name: 'PBKDF2', salt: stableBytes(salt), iterations, hash: 'SHA-256' },
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
    { name: 'AES-GCM', iv: stableBytes(iv) },
    key,
    stableBytes(encoder.encode(JSON.stringify(payload))),
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
  try {
    const key = await deriveKey(password, base64ToBytes(envelope.salt));
    const decrypted = await crypto.subtle.decrypt(
      { name: 'AES-GCM', iv: stableBytes(base64ToBytes(envelope.iv)) },
      key,
      stableBytes(base64ToBytes(envelope.data)),
    );
    return JSON.parse(decoder.decode(decrypted)) as BackupPayload;
  } catch (error) {
    console.error('Falha ao descriptografar backup:', error);
    throw new Error('Senha incorreta ou backup criptografado corrompido.', { cause: error });
  }
}
