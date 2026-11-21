
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
  signOut,
  updateProfile,
} from "firebase/auth";

import { auth } from "./firebase";

export async function registerWithEmail(
  name: string,
  email: string,
  password: string
) {
  // 1. Create the Firebase account
  const credential = await createUserWithEmailAndPassword(
    auth,
    email,
    password
  );

  // 2. Save the user's display name in Firebase
  await updateProfile(credential.user, {
    displayName: name,
  });

  // 3. Get the Firebase ID token
  const idToken = await credential.user.getIdToken();

  await syncUserWithBackend(idToken, name);
  return credential.user;
}

export async function loginWithEmail(email: string, password: string) {
  const credential = await signInWithEmailAndPassword(auth, email, password);
  const idToken = await credential.user.getIdToken();
  await syncUserWithBackend(idToken, credential.user.displayName ?? "");
  return credential.user;
}

export async function logout() {
  await signOut(auth);
}

export async function syncUserWithBackend(idToken: string, displayName: string) {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5159"}/api/auth/sync`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${idToken}` },
    body: JSON.stringify({ displayName }),
  });

  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(payload?.message ?? "Your account could not be synchronized.");
  }

  return response.json();
}

export function getAuthErrorMessage(error: unknown) {
  const code = typeof error === "object" && error !== null && "code" in error
    ? String((error as { code: string }).code)
    : "";

  const messages: Record<string, string> = {
    "auth/email-already-in-use": "An account with this email already exists.",
    "auth/invalid-credential": "The email or password is incorrect.",
    "auth/invalid-email": "Enter a valid email address.",
    "auth/weak-password": "Choose a stronger password (at least 6 characters).",
    "auth/user-disabled": "This account has been disabled.",
    "auth/too-many-requests": "Too many attempts. Please try again later.",
  };

  return messages[code] ?? (error instanceof Error ? error.message : "Something went wrong. Please try again.");
}
