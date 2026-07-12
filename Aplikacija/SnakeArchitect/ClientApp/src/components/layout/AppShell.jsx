import { useAuth } from "../../context/AuthContext.jsx";
import React, { useEffect, useState } from "react";

const DARK_MODE_KEY = "snakeArchitect.darkMode";

function initials(name, fallback) {
  const value = name || fallback || "?";
  const parts = value.split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

function readStoredDarkMode() {
  try {
    return localStorage.getItem(DARK_MODE_KEY) === "1";
  } catch {
    return false;
  }
}

export default function AppShell({
  children,
  currentView,
  onGoHome,
  onGoProfile,
  selectedRoomName,
  socialPanel,
  chatPanel
}) {
  const { logout, user } = useAuth();
  const dashboardActive = currentView === "dashboard";
  const roomActive = currentView === "room";
  const profileActive = currentView === "profile";
  const dashboardClass = "nav-button" + (dashboardActive ? " active" : "");
  const roomClass = "nav-button" + (roomActive ? " active" : "");
  const profileClass = "nav-button" + (profileActive ? " active" : "");
  const [darkMode, setDarkMode] = useState(readStoredDarkMode);

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", darkMode ? "dark" : "light");
    try {
      localStorage.setItem(DARK_MODE_KEY, darkMode ? "1" : "0");
    } catch {}
  }, [darkMode]);

  return (
    <div className="app-shell">
      <header className="app-header">
        <nav className="top-nav" aria-label="Glavna navigacija">
          <button className={dashboardClass} onClick={onGoHome}>
            Pocetna
          </button>
          <button
            className={roomClass}
            onClick={selectedRoomName ? undefined : onGoHome}
            disabled={!selectedRoomName}
          >
            Soba
          </button>
          <button className={profileClass} onClick={onGoProfile}>
            Profil
          </button>
        </nav>

        <h1 className="app-title">Snake Architect</h1>

        <div className="top-account">
          <button
            className="ghost dark-mode-toggle"
            onClick={() => setDarkMode((current) => !current)}
            type="button"
          >
            {darkMode ? "Svetla tema" : "Tamna tema"}
          </button>

          <button className="brand-row brand-row-button" onClick={onGoProfile} type="button">
            <span className="avatar avatar-sm">
              {user?.profilePicture ? (
                <img src={user.profilePicture} alt={user?.username || "Profil"} />
              ) : (
                <span>{initials(user?.name, user?.username)}</span>
              )}
            </span>
            <span>
              <p className="eyebrow">Snake Architect</p>
              <strong>{user?.username}</strong>
            </span>
          </button>

          <button className="ghost sidebar-logout" onClick={logout}>
            Odjavi se
          </button>
        </div>
      </header>

      <aside className="sidebar">
        {socialPanel && (
          <div className="sidebar-social">
            {socialPanel}
          </div>
        )}

        {chatPanel && (
          <div className="sidebar-chat">
            {chatPanel}
          </div>
        )}
      </aside>

      <section className="workspace">{children}</section>
    </div>
  );
}
