import Link from "next/link";

export default function Home() {
  return <main className="flex min-h-screen items-center justify-center bg-slate-950 px-6 py-16"><section className="max-w-2xl text-center text-white"><p className="text-sm font-bold tracking-[0.25em] text-blue-400">TEAM TASK BOARD</p><h1 className="mt-6 text-5xl font-bold tracking-tight sm:text-6xl">Keep your team moving.</h1><p className="mx-auto mt-6 max-w-xl text-lg leading-8 text-slate-300">A focused workspace for planning tasks, sharing progress, and delivering work together.</p><div className="mt-10 flex flex-col justify-center gap-3 sm:flex-row"><Link href="/login" className="rounded-lg bg-blue-600 px-6 py-3 font-semibold hover:bg-blue-500">Sign in</Link><Link href="/register" className="rounded-lg border border-slate-600 px-6 py-3 font-semibold hover:bg-slate-800">Create account</Link></div></section></main>;
}
