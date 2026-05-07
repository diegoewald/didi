import { Moon, Sun } from 'lucide-react';

export function ThemeToggle({ dark, onToggle }: { dark: boolean; onToggle: () => void }) {
  return (
    <button className="btn btn-secondary !px-3 sm:!px-4" onClick={onToggle} aria-label="Alternar tema">
      {dark ? <Sun size={18} /> : <Moon size={18} />}
      <span className="hidden sm:inline">Tema</span>
    </button>
  );
}
