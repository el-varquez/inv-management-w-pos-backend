import { NavLink } from 'react-router-dom';

const tabs = [
  { to: '/items', label: 'Items', end: true },
  { to: '/items/categories', label: 'Categories', end: false },
];

export const ItemsTabs = () => (
  <div className="subnav">
    {tabs.map((tab) => (
      <NavLink
        key={tab.to}
        to={tab.to}
        end={tab.end}
        className={({ isActive }) =>
          isActive ? 'subnav-link is-active' : 'subnav-link'
        }
      >
        {tab.label}
      </NavLink>
    ))}
  </div>
);
