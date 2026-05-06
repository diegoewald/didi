export function DateInput(props: React.InputHTMLAttributes<HTMLInputElement>) { return <input {...props} type="date" className={`input ${props.className ?? ''}`} />; }
