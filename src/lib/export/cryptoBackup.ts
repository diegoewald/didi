import type { BackupPayload, EncryptedBackupEnvelope } from '../../types';

const encoder = new TextEncoder();
const decoder = new TextDecoder();
const iterations = 210_000;
const base64ChunkSize = 0x8000;
export const encryptedBackupUnavailableMessage =
  'Backup criptografado exige navegador compatível ou HTTPS. Use backup simples ou publique online em HTTPS.';

function webCrypto(): Crypto | null {
  return globalThis.crypto ?? null;
}

export function isEncryptedBackupSupported(): boolean {
  const cryptoApi = webCrypto();
  return Boolean(cryptoApi?.subtle && typeof cryptoApi.getRandomValues === 'function' && globalThis.isSecureContext !== false);
}

function requireEncryptedBackupSupport(): Crypto {
  const cryptoApi = webCrypto();
  if (!cryptoApi?.subtle || typeof cryptoApi.getRandomValues !== 'function' || globalThis.isSecureContext === false) {
    throw new Error(encryptedBackupUnavailableMessage);
  }
  return cryptoApi;
}

function stableBytes(bytes: Uint8Array): Uint8Array<ArrayBuffer> {
  const copy = new Uint8Array(bytes.byteLength);
  copy.set(bytes);
  return copy as Uint8Array<ArrayBuffer>;
}

function bytesToBase64(bytes: Uint8Array): string {
  const chunks: string[] = [];
  for (let index = 0; index < bytes.length; index += base64ChunkSize) {
    const chunk = bytes.subarray(index, index + base64ChunkSize);
    let binary = '';
    for (let chunkIndex = 0; chunkIndex < chunk.length; chunkIndex += 1) {
      binary += String.fromCharCode(chunk[chunkIndex]);
    }
    chunks.push(binary);
  }
  return btoa(chunks.join(''));
}

function base64ToBytes(value: string): Uint8Array {
  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }
  return bytes;
}

async function deriveKey(password: string, salt: Uint8Array, cryptoApi: Crypto): Promise<CryptoKey> {
  const baseKey = await cryptoApi.subtle.importKey('raw', stableBytes(encoder.encode(password)), 'PBKDF2', false, [
    'deriveKey',
  ]);
  return cryptoApi.subtle.deriveKey(
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
  const cryptoApi = requireEncryptedBackupSupport();
  const salt = cryptoApi.getRandomValues(new Uint8Array(16));
  const iv = cryptoApi.getRandomValues(new Uint8Array(12));
  const key = await deriveKey(password, salt, cryptoApi);
  const encrypted = await cryptoApi.subtle.encrypt(
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
  const cryptoApi = requireEncryptedBackupSupport();
  try {
    const key = await deriveKey(password, base64ToBytes(envelope.salt), cryptoApi);
    const decrypted = await cryptoApi.subtle.decrypt(
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
