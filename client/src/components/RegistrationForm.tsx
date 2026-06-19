import { useEffect, useState, type FormEvent } from 'react';
import { CHESS_LEVELS, emptyForm, type RegistrationFormData } from '../types/registration';

interface RegistrationFormProps {
  initialValues?: RegistrationFormData;
  submitLabel: string;
  onSubmit: (data: RegistrationFormData) => Promise<void>;
  onCancel?: () => void;
}

export function RegistrationForm({
  initialValues = emptyForm,
  submitLabel,
  onSubmit,
  onCancel,
}: RegistrationFormProps) {
  const [form, setForm] = useState(initialValues);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setForm(initialValues);
  }, [initialValues]);

  function updateField<K extends keyof RegistrationFormData>(
    field: K,
    value: RegistrationFormData[K],
  ) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) {
      return;
    }

    setError(null);
    setSubmitting(true);

    try {
      await onSubmit(form);
    } catch (err) {
      if (err instanceof DOMException && err.name === 'TimeoutError') {
        setError('Lidhja me serverin zgjati shumë. Provoni përsëri — regjistrimi mund të jetë ruajtur.');
      } else {
        setError(err instanceof Error ? err.message : 'Diçka shkoi keq.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      <section className="form-section">
        <h2>Të dhënat e fëmijës</h2>
        <label>
          Emri i plotë
          <input
            required
            value={form.kidFullName}
            onChange={(e) => updateField('kidFullName', e.target.value)}
          />
        </label>
        <label>
          Mosha
          <input
            required
            type="number"
            min={4}
            max={18}
            value={form.kidAge}
            onChange={(e) =>
              updateField('kidAge', e.target.value === '' ? '' : Number(e.target.value))
            }
          />
        </label>
        <label>
          Shkolla
          <input
            required
            value={form.kidSchool}
            onChange={(e) => updateField('kidSchool', e.target.value)}
          />
        </label>
        <label>
          Niveli i shahut
          <select
            required
            value={form.kidChessLevel}
            onChange={(e) => updateField('kidChessLevel', e.target.value)}
          >
            {CHESS_LEVELS.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
        </label>
      </section>

      <section className="form-section">
        <h2>Prindi / kujdestari</h2>
        <label>
          Emri i plotë
          <input
            required
            value={form.parentName}
            onChange={(e) => updateField('parentName', e.target.value)}
          />
        </label>
        <label>
          Numri i telefonit
          <input
            required
            type="tel"
            value={form.parentPhone}
            onChange={(e) => updateField('parentPhone', e.target.value)}
          />
        </label>
        <label>
          Email
          <input
            required
            type="email"
            value={form.parentEmail}
            onChange={(e) => updateField('parentEmail', e.target.value)}
          />
        </label>
      </section>

      {error && <p className="error">{error}</p>}

      <div className="form-actions">
        {onCancel && (
          <button type="button" className="secondary" onClick={onCancel} disabled={submitting}>
            Anulo
          </button>
        )}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Duke ruajtur...' : submitLabel}
        </button>
      </div>
    </form>
  );
}
