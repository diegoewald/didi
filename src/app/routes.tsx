import { BrainCircuit, FileImage, Microscope, ShieldAlert } from 'lucide-react';

export const routes = [
  { path: '/forense', label: 'Análise forense', icon: Microscope },
  { path: '/forense#metadados', label: 'Metadados', icon: FileImage },
  { path: '/forense#evidencias', label: 'Evidências', icon: ShieldAlert },
  { path: '/forense#modelos', label: 'Heurísticas', icon: BrainCircuit },
];

export const quickActions = [{ path: '/forense', label: 'Nova análise', icon: FileImage }];
