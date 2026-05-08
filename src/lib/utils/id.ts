const fallbackPrefix = 'fp';

function randomHexFromValues(length: number): string | null {
  const cryptoApi = globalThis.crypto;
  if (!cryptoApi || typeof cryptoApi.getRandomValues !== 'function') return null;

  try {
    const bytes = new Uint8Array(Math.ceil(length / 2));
    cryptoApi.getRandomValues(bytes);
    return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('').slice(0, length);
  } catch (error) {
    console.warn('Fallback seguro com getRandomValues indisponível.', error);
    return null;
  }
}

function mathRandomId(): string {
  const timestamp = Date.now().toString(36);
  const random = Math.random().toString(36).slice(2, 14);
  return `${fallbackPrefix}-${timestamp}-${random}`;
}

export function createId(): string {
  const cryptoApi = globalThis.crypto;
  if (typeof cryptoApi?.randomUUID === 'function') {
    return cryptoApi.randomUUID();
  }

  const randomHex = randomHexFromValues(32);
  if (randomHex) {
    return `${randomHex.slice(0, 8)}-${randomHex.slice(8, 12)}-4${randomHex.slice(13, 16)}-${(
      (Number.parseInt(randomHex.slice(16, 18), 16) & 0x3f) | 0x80
    ).toString(16)}${randomHex.slice(18, 20)}-${randomHex.slice(20, 32)}`;
  }

  return mathRandomId();
}
