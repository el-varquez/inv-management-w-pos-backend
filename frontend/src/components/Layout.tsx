import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/hooks/useAuth';

export const Layout = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">T</div>
          <div>
            <div className="brand-name">Tindahan</div>
            <div className="brand-sub">POS &amp; Inventory</div>
          </div>
        </div>

        <nav className="nav">
          <NavLink
            to="/items"
            className={({ isActive }) =>
              isActive ? 'nav-link is-active' : 'nav-link'
            }
          >
            Items
          </NavLink>
          <NavLink
            to="/inventory"
            className={({ isActive }) =>
              isActive ? 'nav-link is-active' : 'nav-link'
            }
          >
            Inventory
          </NavLink>
          <NavLink
            to="/sales"
            className={({ isActive }) =>
              isActive ? 'nav-link is-active' : 'nav-link'
            }
          >
            Sales
          </NavLink>
          <span className="nav-soon">
            Reports <span className="soon-tag">soon</span>
          </span>
        </nav>

        <div className="topbar-right">
          <div className="user-chip">
            <div className="user-name">{user?.name ?? 'Signed in'}</div>
            <div className="user-role">{user?.role ?? '—'}</div>
          </div>
          <button className="btn btn-ghost" onClick={handleLogout}>
            Log out
          </button>
        </div>
      </header>

      <main className="page">
        <Outlet />
      </main>
    </div>
  );
};
