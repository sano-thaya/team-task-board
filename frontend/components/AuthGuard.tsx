"use client";

import { useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { useAuth } from "./AuthProvider";

export function AuthGuard({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth(); const router = useRouter();
  useEffect(() => { if (!loading && !user) router.replace("/login"); }, [loading, user, router]);
  if (loading || !user) return <main className="flex min-h-screen items-center justify-center bg-slate-950 px-6 text-slate-300" aria-live="polite"><div className="text-center"><div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-slate-600 border-t-blue-400" aria-hidden="true" /><p className="mt-4 text-sm font-medium">Checking your session…</p></div></main>;
  return <>{children}</>;
}
