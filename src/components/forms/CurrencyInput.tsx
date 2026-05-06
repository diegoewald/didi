export function CurrencyInput(props: React.InputHTMLAttributes<HTMLInputElement>) { return <input {...props} inputMode="decimal" className={`input ${props.className ?? ''}`} placeholder="0,00" />; }
