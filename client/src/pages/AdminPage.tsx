import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ApiError,
  createRegistrationManual,
  exportRegistrationsCsv,
  fetchRegistrations,
  updateRegistration,
  validateAdminKey,
} from '../api/registrations';
import { RegistrationForm } from '../components/RegistrationForm';
import { emptyFilters, type RegistrationFilters } from '../types/filters';
import {
  CHESS_LEVELS,
  emptyForm,
  type Registration,
  type RegistrationFormData,
} from '../types/registration';

type AuthStatus = 'login' | 'validating' | 'authenticated';

function toFormData(registration: Registration): RegistrationFormData {
  return {
    kidFullName: registration.kidFullName,
    kidAge: registration.kidAge,
    kidSchool: registration.kidSchool,
    kidChessLevel: registration.kidChessLevel,
    parentName: registration.parentName,
    parentPhone: registration.parentPhone,
    parentEmail: registration.parentEmail,
  };
}

export function AdminPage() {
  const [adminKey, setAdminKey] = useState('');
  const [storedKey, setStoredKey] = useState('');
  const [authStatus, setAuthStatus] = useState<AuthStatus>('validating');
  const [loginError, setLoginError] = useState<string | null>(null);
  const [loggingIn, setLoggingIn] = useState(false);
  const [registrations, setRegistrations] = useState<Registration[]>([]);
  const [filters, setFilters] = useState<RegistrationFilters>(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState<RegistrationFilters>(emptyFilters);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<'list' | 'create' | 'edit'>('list');
  const [selected, setSelected] = useState<Registration | null>(null);

  const logout = useCallback((message?: string) => {
    sessionStorage.removeItem('adminKey');
    setAdminKey('');
    setStoredKey('');
    setAuthStatus('login');
    setRegistrations([]);
    setFilters(emptyFilters);
    setAppliedFilters(emptyFilters);
    setMode('list');
    setSelected(null);
    setError(null);
    if (message) {
      setLoginError(message);
    }
  }, []);

  const loadRegistrations = useCallback(
    async (key: string, nextFilters: RegistrationFilters) => {
      setLoading(true);
      setError(null);

      try {
        const data = await fetchRegistrations(key, nextFilters);
        setRegistrations(data);
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          logout('Sesioni juaj nuk është i vlefshëm. Ju lutemi identifikohuni përsëri.');
          return;
        }

        if (err instanceof DOMException && err.name === 'TimeoutError') {
          setError('Lidhja me serverin zgjati shumë. Provoni përsëri.');
        } else {
          setError(err instanceof Error ? err.message : 'Ngarkimi i regjistrimeve dështoi.');
        }
        setRegistrations([]);
      } finally {
        setLoading(false);
      }
    },
    [logout],
  );

  useEffect(() => {
    const savedKey = sessionStorage.getItem('adminKey');

    if (!savedKey) {
      setAuthStatus('login');
      return;
    }

    void validateAdminKey(savedKey)
      .then(() => {
        setStoredKey(savedKey);
        setAuthStatus('authenticated');
      })
      .catch((err) => {
        sessionStorage.removeItem('adminKey');
        setAuthStatus('login');

        if (err instanceof ApiError && err.status === 503) {
          setLoginError('Aksesi i administratorit nuk është konfiguruar në server.');
        } else if (err instanceof ApiError && err.status === 401) {
          setLoginError('Sesioni i ruajtur skadoi. Ju lutemi identifikohuni përsëri.');
        } else if (err instanceof DOMException && err.name === 'TimeoutError') {
          setLoginError('Lidhja me serverin zgjati shumë. Provoni përsëri.');
        } else {
          setLoginError(err instanceof Error ? err.message : 'Verifikimi i sesionit dështoi.');
        }
      });
  }, []);

  useEffect(() => {
    if (authStatus === 'authenticated' && storedKey) {
      void loadRegistrations(storedKey, appliedFilters);
    }
  }, [authStatus, storedKey, appliedFilters, loadRegistrations]);

  async function handleLogin(event: React.FormEvent) {
    event.preventDefault();
    setLoggingIn(true);
    setLoginError(null);

    try {
      await validateAdminKey(adminKey);
      sessionStorage.setItem('adminKey', adminKey);
      setStoredKey(adminKey);
      setAuthStatus('authenticated');
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setLoginError('Çelësi i administratorit është i pavlefshëm.');
      } else if (err instanceof ApiError && err.status === 503) {
        setLoginError('Aksesi i administratorit nuk është konfiguruar në server.');
      } else if (err instanceof DOMException && err.name === 'TimeoutError') {
        setLoginError('Lidhja me serverin zgjati shumë. Provoni përsëri.');
      } else {
        setLoginError(err instanceof Error ? err.message : 'Verifikimi i çelësit dështoi.');
      }
    } finally {
      setLoggingIn(false);
    }
  }

  function handleLogout() {
    logout();
    setLoginError(null);
  }

  function updateFilter<K extends keyof RegistrationFilters>(field: K, value: RegistrationFilters[K]) {
    setFilters((current) => ({ ...current, [field]: value }));
  }

  function handleApplyFilters(event: React.FormEvent) {
    event.preventDefault();
    setAppliedFilters(filters);
  }

  function handleClearFilters() {
    setFilters(emptyFilters);
    setAppliedFilters(emptyFilters);
  }

  async function handleExportCsv() {
    if (!storedKey) return;

    setExporting(true);
    setError(null);

    try {
      await exportRegistrationsCsv(storedKey, appliedFilters);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        logout('Sesioni juaj nuk është i vlefshëm. Ju lutemi identifikohuni përsëri.');
        return;
      }

      if (err instanceof DOMException && err.name === 'TimeoutError') {
        setError('Lidhja me serverin zgjati shumë. Provoni përsëri.');
      } else {
        setError(err instanceof Error ? err.message : 'Eksportimi CSV dështoi.');
      }
    } finally {
      setExporting(false);
    }
  }

  async function handleCreate(data: RegistrationFormData) {
    if (!storedKey) return;

    try {
      await createRegistrationManual(storedKey, data);
      await loadRegistrations(storedKey, appliedFilters);
      setMode('list');
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        logout('Sesioni juaj nuk është i vlefshëm. Ju lutemi identifikohuni përsëri.');
      } else {
        throw err;
      }
    }
  }

  async function handleUpdate(data: RegistrationFormData) {
    if (!storedKey || !selected) return;

    try {
      await updateRegistration(storedKey, selected.id, data);
      await loadRegistrations(storedKey, appliedFilters);
      setMode('list');
      setSelected(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        logout('Sesioni juaj nuk është i vlefshëm. Ju lutemi identifikohuni përsëri.');
      } else {
        throw err;
      }
    }
  }

  const editValues = useMemo(
    () => (selected ? toFormData(selected) : emptyForm),
    [selected],
  );

  const hasActiveFilters = useMemo(
    () =>
      Boolean(
        appliedFilters.search ||
          appliedFilters.chessLevel ||
          appliedFilters.minAge !== '' ||
          appliedFilters.maxAge !== '',
      ),
    [appliedFilters],
  );

  if (authStatus === 'validating') {
    return (
      <div className="page narrow">
        <header className="page-header">
          <h1>Identifikimi i administratorit</h1>
          <p>Duke kontrolluar sesionin tuaj...</p>
        </header>
      </div>
    );
  }

  if (authStatus === 'login') {
    return (
      <div className="page narrow">
        <header className="page-header">
          <h1>Identifikimi i administratorit</h1>
          <p>Vendosni çelësin e administratorit për të menaxhuar regjistrimet.</p>
        </header>
        <form className="form" onSubmit={handleLogin}>
          <label>
            Çelësi i administratorit
            <input
              required
              type="password"
              value={adminKey}
              onChange={(e) => setAdminKey(e.target.value)}
            />
          </label>
          {loginError && <p className="error">{loginError}</p>}
          <button type="submit" disabled={loggingIn}>
            {loggingIn ? 'Duke verifikuar...' : 'Hyr në panelin e administratorit'}
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="page wide">
      <header className="page-header admin-header">
        <div>
          <h1>Paneli i administratorit</h1>
          <p>Shikoni, filtroni, eksportoni, shtoni dhe përditësoni regjistrimet e kampit të shahut.</p>
        </div>
        <div className="admin-actions">
          {mode === 'list' && (
            <>
              <button type="button" onClick={() => setMode('create')}>
                Shto regjistrim
              </button>
              <button
                type="button"
                className="secondary"
                onClick={handleExportCsv}
                disabled={exporting || registrations.length === 0}
              >
                {exporting ? 'Duke eksportuar...' : 'Eksporto CSV'}
              </button>
            </>
          )}
          <button type="button" className="secondary" onClick={handleLogout}>
            Dil
          </button>
        </div>
      </header>

      {error && <p className="error">{error}</p>}

      {mode === 'create' && (
        <RegistrationForm
          initialValues={emptyForm}
          submitLabel="Krijo regjistrimin"
          onSubmit={handleCreate}
          onCancel={() => setMode('list')}
        />
      )}

      {mode === 'edit' && selected && (
        <RegistrationForm
          initialValues={editValues}
          submitLabel="Ruaj ndryshimet"
          onSubmit={handleUpdate}
          onCancel={() => {
            setMode('list');
            setSelected(null);
          }}
        />
      )}

      {mode === 'list' && (
        <>
          <form className="form filters-form" onSubmit={handleApplyFilters}>
            <div className="filters-grid">
              <label>
                Kërko
                <input
                  placeholder="Emër, shkollë, prind, email, telefon"
                  value={filters.search}
                  onChange={(e) => updateFilter('search', e.target.value)}
                />
              </label>
              <label>
                Niveli i shahut
                <select
                  value={filters.chessLevel}
                  onChange={(e) => updateFilter('chessLevel', e.target.value)}
                >
                  <option value="">Të gjitha nivelet</option>
                  {CHESS_LEVELS.map((level) => (
                    <option key={level} value={level}>
                      {level}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Mosha min.
                <input
                  type="number"
                  min={4}
                  max={18}
                  value={filters.minAge}
                  onChange={(e) => updateFilter('minAge', e.target.value)}
                />
              </label>
              <label>
                Mosha max.
                <input
                  type="number"
                  min={4}
                  max={18}
                  value={filters.maxAge}
                  onChange={(e) => updateFilter('maxAge', e.target.value)}
                />
              </label>
            </div>
            <div className="form-actions">
              <button type="button" className="secondary" onClick={handleClearFilters}>
                Pastro filtrat
              </button>
              <button type="submit">Apliko filtrat</button>
            </div>
          </form>

          <section className="table-section">
            <div className="table-meta">
              <p>
                Po shfaqen {registrations.length} regjistrim{registrations.length === 1 ? '' : 'e'}
                {hasActiveFilters ? ' (të filtruara)' : ''}
              </p>
            </div>

            {loading ? (
              <p>Duke ngarkuar regjistrimet...</p>
            ) : registrations.length === 0 ? (
              <p>
                {hasActiveFilters
                  ? 'Asnjë regjistrim nuk përputhet me filtrat tuaj.'
                  : 'Ende nuk ka regjistrime.'}
              </p>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Fëmija</th>
                      <th>Mosha</th>
                      <th>Shkolla</th>
                      <th>Niveli</th>
                      <th>Prindi</th>
                      <th>Telefoni</th>
                      <th>Email</th>
                      <th>Regjistruar</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {registrations.map((registration) => (
                      <tr key={registration.id}>
                        <td>{registration.kidFullName}</td>
                        <td>{registration.kidAge}</td>
                        <td>{registration.kidSchool}</td>
                        <td>{registration.kidChessLevel}</td>
                        <td>{registration.parentName}</td>
                        <td>{registration.parentPhone}</td>
                        <td>{registration.parentEmail}</td>
                        <td>{new Date(registration.createdAt).toLocaleString('sq-AL')}</td>
                        <td>
                          <button
                            type="button"
                            className="link-button"
                            onClick={() => {
                              setSelected(registration);
                              setMode('edit');
                            }}
                          >
                            Ndrysho
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
