import type { AuthResponse } from './authApi';

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

export type AuthUser = {
  id: string;
  email: string;
  fullName: string;
};

type JwtPayload = {
  exp?: number;
};

function isValidToken(token: string | null): boolean {
  if (!token) {
    return false;
  }

  const parts = token.split('.');
  if (parts.length !== 3) {
    return false;
  }

  try {
    const payloadBase64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const paddedPayload = payloadBase64.padEnd(Math.ceil(payloadBase64.length / 4) * 4, '=');
    const binaryPayload = atob(paddedPayload);
    const payloadBytes = Uint8Array.from(binaryPayload, (character) => character.charCodeAt(0));
    const payload = JSON.parse(new TextDecoder().decode(payloadBytes)) as JwtPayload;

    return typeof payload.exp === 'number' && payload.exp * 1000 > Date.now();
  } catch {
    return false;
  }
}

function getActiveStorage(): Storage | null {
  const storages = [localStorage, sessionStorage];

  return storages.find((storage) => isValidToken(storage.getItem(TOKEN_KEY))) ?? null;
}

export function isAuthenticated(): boolean {
  return getActiveStorage() !== null;
}

export function getAuthUser(): AuthUser | null {
  const storage = getActiveStorage();
  const userJson = storage?.getItem(USER_KEY);

  if (!userJson) {
    return null;
  }

  try {
    const user = JSON.parse(userJson) as Partial<AuthUser>;
    if (typeof user.email !== 'string' || typeof user.fullName !== 'string') {
      return null;
    }

    return {
      id: typeof user.id === 'string' ? user.id : '',
      email: user.email,
      fullName: user.fullName,
    };
  } catch {
    return null;
  }
}

export function saveAuthSession(response: AuthResponse, persist: boolean): void {
  clearAuthSession();

  const storage = persist ? localStorage : sessionStorage;
  storage.setItem(TOKEN_KEY, response.token);
  storage.setItem(USER_KEY, JSON.stringify({
    id: response.userId,
    email: response.email,
    fullName: response.fullName,
  } satisfies AuthUser));
}

export function clearAuthSession(): void {
  [localStorage, sessionStorage].forEach((storage) => {
    storage.removeItem(TOKEN_KEY);
    storage.removeItem(USER_KEY);
  });
}
