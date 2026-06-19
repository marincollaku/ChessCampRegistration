import type { Registration, RegistrationFormData } from '../types/registration';
import { buildFilterQuery, type RegistrationFilters } from '../types/filters';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5075';
const DEFAULT_TIMEOUT_MS = 60_000;

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

type ProblemDetails = {
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
};

async function parseErrorMessage(response: Response): Promise<string> {
  const text = await response.text();

  try {
    const json = JSON.parse(text) as ProblemDetails;

    if (json.message) {
      return json.message;
    }

    if (json.errors) {
      const messages = Object.values(json.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (json.title) {
      return json.title;
    }

    return text;
  } catch {
    return text || `Kërkesa dështoi me status ${response.status}`;
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(await parseErrorMessage(response), response.status);
  }

  return response.json() as Promise<T>;
}

function fetchWithTimeout(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  return fetch(input, {
    ...init,
    signal: init?.signal ?? AbortSignal.timeout(DEFAULT_TIMEOUT_MS),
  });
}

function toPayload(data: RegistrationFormData) {
  if (data.kidAge === '') {
    throw new ApiError('Mosha është e detyrueshme.', 400);
  }

  return {
    kidFullName: data.kidFullName.trim(),
    kidAge: data.kidAge,
    kidSchool: data.kidSchool.trim(),
    kidChessLevel: data.kidChessLevel.trim(),
    parentName: data.parentName.trim(),
    parentPhone: data.parentPhone.trim(),
    parentEmail: data.parentEmail.trim(),
  };
}

export async function validateAdminKey(adminKey: string): Promise<void> {
  const response = await fetchWithTimeout(`${API_BASE}/api/admin/registrations`, {
    headers: { 'X-Admin-Key': adminKey },
  });

  if (!response.ok) {
    throw new ApiError(await parseErrorMessage(response), response.status);
  }
}

export async function submitRegistration(data: RegistrationFormData): Promise<Registration> {
  const response = await fetchWithTimeout(`${API_BASE}/api/registrations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(toPayload(data)),
  });

  return handleResponse<Registration>(response);
}

export async function fetchRegistrations(
  adminKey: string,
  filters: RegistrationFilters = {
    search: '',
    chessLevel: '',
    minAge: '',
    maxAge: '',
  },
): Promise<Registration[]> {
  const query = buildFilterQuery(filters);
  const response = await fetchWithTimeout(`${API_BASE}/api/admin/registrations${query}`, {
    headers: { 'X-Admin-Key': adminKey },
  });

  return handleResponse<Registration[]>(response);
}

export async function exportRegistrationsCsv(
  adminKey: string,
  filters: RegistrationFilters,
): Promise<void> {
  const query = buildFilterQuery(filters);
  const response = await fetchWithTimeout(`${API_BASE}/api/admin/registrations/export${query}`, {
    headers: { 'X-Admin-Key': adminKey },
  });

  if (!response.ok) {
    throw new ApiError(await parseErrorMessage(response), response.status);
  }

  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition');
  const fileName =
    disposition?.match(/filename="?([^"]+)"?/)?.[1] ??
    `regjistrimet-kampi-shahut-${new Date().toISOString().slice(0, 10)}.csv`;

  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function createRegistrationManual(
  adminKey: string,
  data: RegistrationFormData,
): Promise<Registration> {
  const response = await fetchWithTimeout(`${API_BASE}/api/admin/registrations`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Admin-Key': adminKey,
    },
    body: JSON.stringify(toPayload(data)),
  });

  return handleResponse<Registration>(response);
}

export async function updateRegistration(
  adminKey: string,
  id: number,
  data: RegistrationFormData,
): Promise<Registration> {
  const response = await fetchWithTimeout(`${API_BASE}/api/admin/registrations/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'X-Admin-Key': adminKey,
    },
    body: JSON.stringify(toPayload(data)),
  });

  return handleResponse<Registration>(response);
}
