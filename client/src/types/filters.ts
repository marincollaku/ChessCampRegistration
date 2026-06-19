export interface RegistrationFilters {
  search: string;
  chessLevel: string;
  minAge: string;
  maxAge: string;
}

export const emptyFilters: RegistrationFilters = {
  search: '',
  chessLevel: '',
  minAge: '',
  maxAge: '',
};

export function buildFilterQuery(filters: RegistrationFilters): string {
  const params = new URLSearchParams();

  if (filters.search.trim()) {
    params.set('search', filters.search.trim());
  }

  if (filters.chessLevel) {
    params.set('chessLevel', filters.chessLevel);
  }

  if (filters.minAge !== '') {
    params.set('minAge', String(filters.minAge));
  }

  if (filters.maxAge !== '') {
    params.set('maxAge', String(filters.maxAge));
  }

  const query = params.toString();
  return query ? `?${query}` : '';
}
