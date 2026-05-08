import type { InputHTMLAttributes } from 'react';

export function DateInput(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} type="date" className={`input ${props.className ?? ''}`} />;
}
