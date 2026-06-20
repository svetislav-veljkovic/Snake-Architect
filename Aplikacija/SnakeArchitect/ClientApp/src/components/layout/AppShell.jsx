import { useAuth } from "../../context/AuthContext.jsx";
import React from "react";

export default function AppShell({
  children,
  currentView,
  gameRequestCount,
  onGoHome,
  selectedRoomName
}) {
  const { logout, user } = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-row">
          <div className="brand-mark small">SA</div>
          <div>
            <p className="eyebrow">Snake Architect</p>
            <strong>{user?.username}</strong>
          </div>
        </div>

        <div className="sidebar-block">
          <button
            className={`nav-button ${currentView === "dashboard" ? "active" : ""}`}
            onClick={onGoHome}
          >
            Pocetna
          </button>
          <button
            className={`nav-button ${currentView === "room" ? "active" : ""}`}
            onClick={selectedRoomName ? undefined : onGoHome}
            disabled={!selectedRoomName}
          >
            Soba
          </button>
        </div>

        <div className="sidebar-note">
          <span>{gameRequestCount}</span>
          <p>aktivnih poziva i zahteva za igru</p>
        </div>

        {selectedRoomName && (
          <div className="sidebar-note soft">
            <span>Live</span>
            <p>{selectedRoomName}</p>
          </div>
        )}

        <button className="ghost sidebar-logout" onClick={logout}>
          Odjavi se
        </button>
      </aside>

      <section className="workspace">{children}</section>
    </div>
  );
}
