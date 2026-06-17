import { NavLink } from 'react-router-dom';

const tabs = [
  { to: '/sales/pos', label: 'Register' },
  { to: '/sales/history', label: 'History' },
];

export const SalesTabs = () => (
  <div className="subnav">
    {tabs.map((tab) => (
      <NavLink
        key={tab.to}
        to={tab.to}
        className={({ isActive }) =>
          isActive ? 'subnav-link is-active' : 'subnav-link'
        }
      >
        {tab.label}
      </NavLink>
    ))}
  </div>
);
