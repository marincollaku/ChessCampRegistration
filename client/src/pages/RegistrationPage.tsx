import { useState } from 'react';
import { submitRegistration } from '../api/registrations';
import { RegistrationForm } from '../components/RegistrationForm';
import { emptyForm, type RegistrationFormData } from '../types/registration';

export function RegistrationPage() {
  const [success, setSuccess] = useState<string | null>(null);
  const [formKey, setFormKey] = useState(0);

  async function handleSubmit(data: RegistrationFormData) {
    await submitRegistration(data);
    setSuccess(
      `Regjistrimi u dërgua për ${data.kidFullName}. Një email konfirmimi do t'i dërgohet ${data.parentEmail}.`,
    );
    setFormKey((key) => key + 1);
  }

  return (
    <div className="page">
      <header className="page-header">
        <h1>Regjistrimi në Kampin e Shahut</h1>
        <p>Regjistroni fëmijën tuaj për kampin e ardhshëm të shahut.</p>
      </header>

      {success ? (
        <div className="success-card">
          <p>{success}</p>
          <button type="button" onClick={() => setSuccess(null)}>
            Regjistro një fëmijë tjetër
          </button>
        </div>
      ) : (
        <RegistrationForm
          key={formKey}
          initialValues={emptyForm}
          submitLabel="Dërgo regjistrimin"
          onSubmit={handleSubmit}
        />
      )}
    </div>
  );
}
