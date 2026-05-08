import type { InputHTMLAttributes } from 'react';

export function CurrencyInput(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} inputMode="decimal" className={`input ${props.className ?? ''}`} placeholder={props.placeholder ?? '0,00'} />;
}
