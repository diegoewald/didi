'use client';

import { useEffect, useMemo, useState } from 'react';
import { AppLayout } from '../components/layout/AppLayout';
import { ErrorBoundary } from '../components/ui/ErrorBoundary';
import { ForensicsPage } from '../features/forensics/ForensicsPage';

function currentPath() {
  if (typeof window === 'undefined') return '/forense';
  return window.location.pathname === '/' ? '/forense' : window.location.pathname;
}

export default function App() {
  const [path, setPath] = useState('/forense');
  const [dark, setDark] = useState(false);

  useEffect(() => {
    setPath(currentPath());
  }, []);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
  }, [dark]);

  useEffect(() => {
    const updatePath = () => setPath(currentPath());
    window.addEventListener('popstate', updatePath);
    return () => window.removeEventListener('popstate', updatePath);
  }, []);

  const navigate = (nextPath: string) => {
    history.pushState(null, '', nextPath);
    setPath(nextPath);
  };

  const Page = useMemo(() => ({ '/forense': ForensicsPage }[path] ?? ForensicsPage), [path]);

  return <ErrorBoundary><AppLayout current={path} navigate={navigate} dark={dark} toggleDark={() => setDark(value => !value)}><Page /></AppLayout></ErrorBoundary>;
}
