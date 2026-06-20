import { useAuth } from "../../context/AuthContext.jsx";
import React from "react";

export default function AppShell({
  children,
  currentView,
  onGoHome,
  selectedRoomName,
  chatPanel
}) {
  const { logout, user } = useAuth();
  const dashboardActive = currentView === "dashboard";
  const roomActive = currentView === "room";
  const dashboardClass = "nav-button" + (dashboardActive ? " active" : "");
  const roomClass = "nav-button" + (roomActive ? " active" : "");

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
            className={dashboardClass}
            onClick={onGoHome}
          >
            Pocetna
          </button>
          <button
            className={roomClass}
            onClick={selectedRoomName ? undefined : onGoHome}
            disabled={!selectedRoomName}
          >
            Soba
          </button>
        </div>

        {chatPanel && (
          <div className="sidebar-chat">
            {chatPanel}
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
