"use client";

import { FormEvent, useEffect, useState } from "react";
import { createComment, getComments, subscribeToCommentUpdates, TaskComment } from "../lib/comments";

export function TaskComments({ projectId, taskId }: { projectId: number; taskId: number }) {
  const [comments, setComments] = useState<TaskComment[]>([]);
  const [text, setText] = useState("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    let active = true;
    const load = () => void getComments(projectId, taskId)
      .then(items => { if (active) { setComments(items); setLoading(false); } })
      .catch(reason => { if (active) { setError(reason instanceof Error ? reason.message : "Could not load comments."); setLoading(false); } });
    load();
    const unsubscribe = subscribeToCommentUpdates(projectId, taskId, load, reason => { if (active) setError(`Live comment updates are unavailable: ${reason.message}`); });
    return () => { active = false; unsubscribe(); };
  }, [projectId, taskId]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!text.trim()) { setError("Comment text is required."); return; }
    setSending(true); setError(""); setSuccess("");
    try { await createComment(projectId, taskId, text.trim()); setText(""); setSuccess("Comment posted."); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not post comment."); }
    finally { setSending(false); }
  }

  return <section className="mt-6 border-t border-slate-200 pt-5" aria-labelledby="comments-heading"><div className="flex items-center justify-between gap-3"><div><p className="text-xs font-bold uppercase tracking-wider text-blue-800">Communication</p><h4 id="comments-heading" className="mt-1 text-lg font-bold text-slate-950">Comments</h4></div><span className="text-xs font-medium text-slate-500">Live updates enabled</span></div>{loading ? <p className="mt-4 text-sm text-slate-600">Loading comments…</p> : comments.length === 0 ? <p className="mt-4 rounded-lg border border-dashed border-slate-300 bg-white p-4 text-sm text-slate-600">No comments yet. Start the conversation.</p> : <div className="mt-4 space-y-3">{comments.map(comment => <article key={comment.id} className="rounded-lg border border-slate-200 bg-white p-4"><div className="flex flex-wrap items-baseline justify-between gap-2"><p className="text-sm font-bold text-slate-900">{comment.authorName}</p><time dateTime={comment.createdAtUtc} className="text-xs text-slate-500">{new Date(comment.createdAtUtc).toLocaleString()}</time></div><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">{comment.text}</p></article>)}</div>}{error && <p role="alert" className="mt-4 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-800">{error}</p>}{success && <p role="status" className="mt-4 text-sm font-medium text-emerald-700">{success}</p>}<form onSubmit={submit} className="mt-4"><label htmlFor="comment-text" className="text-sm font-bold text-slate-800">Add a comment</label><textarea id="comment-text" maxLength={2000} value={text} onChange={event => setText(event.target.value)} placeholder="Share an update or ask a question" rows={3} className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-500 outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-200" /><button disabled={sending} className="mt-3 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-bold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-400">{sending ? "Posting…" : "Post comment"}</button></form></section>;
}
