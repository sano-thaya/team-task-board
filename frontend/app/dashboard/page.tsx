"use client";

import { useRouter } from "next/navigation";
import { AuthGuard } from "../../components/AuthGuard";
import { useAuth } from "../../components/AuthProvider";
import { getAuthErrorMessage, logout } from "../../lib/auth";
import Link from "next/link";
import { useState } from "react";

function DashboardContent() {
  const { user } = useAuth(); const router = useRouter(); const [error, setError] = useState("");
  async function handleLogout() { try { await logout(); router.replace("/login"); } catch (reason) { setError(getAuthErrorMessage(reason)); } }
  return <main className="min-h-screen bg-slate-100"><header className="border-b border-slate-200 bg-white"><div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4"><span className="font-bold tracking-wide text-slate-900">TEAM TASK BOARD</span><button onClick={handleLogout} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">Sign out</button></div></header><section className="mx-auto max-w-6xl px-6 py-16"><p className="text-sm font-semibold uppercase tracking-wider text-blue-600">Dashboard</p><h1 className="mt-3 text-4xl font-bold text-slate-900">Welcome, {user?.displayName || user?.email}</h1><p className="mt-3 text-slate-600">Your authenticated task workspace is ready.</p>{error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}<div className="mt-10 grid gap-4 sm:grid-cols-2"><Link href="/projects" className="rounded-xl bg-white p-6 shadow-sm hover:shadow-md"><h2 className="font-semibold text-slate-900">Projects</h2><p className="mt-2 text-sm text-slate-600">Create and manage your team projects.</p></Link><div className="rounded-xl bg-white p-6 shadow-sm"><h2 className="font-semibold text-slate-900">Authentication status</h2><p className="mt-2 text-sm text-slate-600">Signed in as {user?.email}</p></div></div></section></main>;
}

export default function DashboardPage() { return <AuthGuard><DashboardContent /></AuthGuard>; }
