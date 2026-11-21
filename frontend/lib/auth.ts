
import {
  createUserWithEmailAndPassword,
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

  // 4. Return information needed by the frontend
  return {
    uid: credential.user.uid,
    email: credential.user.email,
    displayName: credential.user.displayName,
    idToken,
  };
}
