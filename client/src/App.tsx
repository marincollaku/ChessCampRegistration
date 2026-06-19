import { Link, Route, Routes } from 'react-router-dom';
import { AdminPage } from './pages/AdminPage';
import { RegistrationPage } from './pages/RegistrationPage';
import './App.css';

function App() {
  return (
    <div className="app-shell">
      <nav className="top-nav">
        <Link to="/" className="brand">
          Kampi i Shahut
        </Link>
        <div className="nav-links">
          <Link to="/">Regjistrohu</Link>
          <Link to="/admin">Admin</Link>
        </div>
      </nav>

      <main>
        <Routes>
          <Route path="/" element={<RegistrationPage />} />
          <Route path="/admin" element={<AdminPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
