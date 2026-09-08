import { NavLink, Outlet } from 'react-router-dom'
import { navigationItems } from '../../app/navigation/navigationItems'
import './AppLayout.css'

function AppLayout() {
    return (
        <div className="app-layout">
            <header className="app-layout__header">
                <div className="app-layout__brand">
                    <span className="app-layout__brand-mark">PC</span>

                    <div>
                        <p className="app-layout__brand-name">PrivacyComply</p>
                        <p className="app-layout__brand-caption">
                            Privacy Compliance Control Plane
                        </p>
                    </div>
                </div>
            </header>

            <div className="app-layout__body">
                <aside className="app-layout__sidebar">
                    <nav
                        className="app-layout__navigation"
                        aria-label="Primary navigation"
                    >
                        {navigationItems.map((item) => (
                            <NavLink
                                key={item.id}
                                to={item.path}
                                className={({ isActive }) =>
                                    isActive
                                        ? 'app-layout__navigation-link app-layout__navigation-link--active'
                                        : 'app-layout__navigation-link'
                                }
                            >
                                {item.label}
                            </NavLink>
                        ))}
                    </nav>
                </aside>

                <main className="app-layout__main">
                    <div className="app-layout__content">
                        <Outlet />
                    </div>
                </main>
            </div>
        </div>
    )
}

export default AppLayout