import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/hooks/useAuth';

export const Layout = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const isAdmin = user?.role === 'Admin';

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const isSuperAdmin = user?.role === 'SuperAdmin';

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
          {isSuperAdmin ? (
            <NavLink
              to="/platform"
              className={({ isActive }) =>
                isActive ? 'nav-link is-active' : 'nav-link'
              }
            >
              Tenants
            </NavLink>
          ) : (
            <>
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
              <NavLink
                to="/reports"
                className={({ isActive }) =>
                  isActive ? 'nav-link is-active' : 'nav-link'
                }
              >
                Reports
              </NavLink>
              {isAdmin && (
                <NavLink
                  to="/cashiers"
                  className={({ isActive }) =>
                    isActive ? 'nav-link is-active' : 'nav-link'
                  }
                >
                  Cashiers
                </NavLink>
              )}
            </>
          )}
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
