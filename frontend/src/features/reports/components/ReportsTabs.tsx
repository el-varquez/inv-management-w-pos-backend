import { NavLink } from 'react-router-dom';

const tabs = [
  { to: '/reports/sales', label: 'Sales' },
  { to: '/reports/expenses', label: 'Expenses' },
  { to: '/reports/profit', label: 'Profit' },
];

export const ReportsTabs = () => (
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
