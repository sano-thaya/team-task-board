"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { getAuthErrorMessage, loginWithEmail } from "../../lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setSubmitting(true);
    try { await loginWithEmail(email.trim(), password); router.replace("/dashboard"); }
    catch (reason) { setError(getAuthErrorMessage(reason)); }
    finally { setSubmitting(false); }
  }

  return <AuthShell title="Welcome back" subtitle="Sign in to continue managing your team tasks.">
    <form onSubmit={handleSubmit} className="space-y-5">
      <Field label="Email" type="email" value={email} onChange={setEmail} required />
      <Field label="Password" type="password" value={password} onChange={setPassword} required />
      {error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <button disabled={submitting} className="w-full rounded-lg bg-blue-600 px-4 py-3 font-semibold text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60">{submitting ? "Signing in…" : "Sign in"}</button>
      <p className="text-center text-sm text-slate-500">New here? <Link className="font-semibold text-blue-600 hover:underline" href="/register">Create an account</Link></p>
    </form>
  </AuthShell>;
}

function Field({ label, type, value, onChange, required }: { label: string; type: string; value: string; onChange: (value: string) => void; required?: boolean }) {
  return <label className="block text-sm font-medium text-slate-700">{label}<input required={required} type={type} value={value} onChange={(event) => onChange(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none ring-blue-500 focus:ring-2" /></label>;
}

function AuthShell({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-12"><section className="w-full max-w-md rounded-2xl bg-white p-8 shadow-2xl"><Link href="/" className="text-sm font-bold tracking-wide text-blue-600">TEAM TASK BOARD</Link><h1 className="mt-8 text-3xl font-bold text-slate-900">{title}</h1><p className="mt-2 mb-8 text-slate-500">{subtitle}</p>{children}</section></main>;
}
