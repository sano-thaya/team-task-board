"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { getAuthErrorMessage, registerWithEmail } from "../../lib/auth";

export default function RegisterPage() {
  const router = useRouter(); const [name, setName] = useState(""); const [email, setEmail] = useState(""); const [password, setPassword] = useState(""); const [confirm, setConfirm] = useState(""); const [error, setError] = useState(""); const [submitting, setSubmitting] = useState(false);
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError("");
    if (name.trim().length < 2) return setError("Enter your full name.");
    if (password !== confirm) return setError("Passwords do not match.");
    setSubmitting(true);
    try { await registerWithEmail(name.trim(), email.trim(), password); router.replace("/dashboard"); }
    catch (reason) { setError(getAuthErrorMessage(reason)); }
    finally { setSubmitting(false); }
  }
  return <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-12"><section className="w-full max-w-md rounded-2xl bg-white p-8 shadow-2xl"><Link href="/" className="text-sm font-bold tracking-wide text-blue-600">TEAM TASK BOARD</Link><h1 className="mt-8 text-3xl font-bold text-slate-900">Create your account</h1><p className="mt-2 mb-8 text-slate-500">Start organizing your team’s work in one place.</p><form onSubmit={handleSubmit} className="space-y-5"><label className="block text-sm font-medium text-slate-700">Full name<input required minLength={2} maxLength={100} value={name} onChange={(e) => setName(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500" /></label><label className="block text-sm font-medium text-slate-700">Email<input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500" /></label><label className="block text-sm font-medium text-slate-700">Password<input required minLength={6} type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500" /></label><label className="block text-sm font-medium text-slate-700">Confirm password<input required type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500" /></label>{error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}<button disabled={submitting} className="w-full rounded-lg bg-blue-600 px-4 py-3 font-semibold text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60">{submitting ? "Creating account…" : "Create account"}</button><p className="text-center text-sm text-slate-500">Already registered? <Link className="font-semibold text-blue-600 hover:underline" href="/login">Sign in</Link></p></form></section></main>;
}
