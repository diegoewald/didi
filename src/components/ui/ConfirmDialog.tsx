export function confirmStrong(message: string, expected = 'APAGAR'): boolean { const result = window.prompt(`${message}\nDigite ${expected} para confirmar.`); return result === expected; }
