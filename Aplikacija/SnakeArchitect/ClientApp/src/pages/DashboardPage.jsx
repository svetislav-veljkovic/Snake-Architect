import { useCallback, useEffect, useMemo, useState } from "react";
import React from "react";
import ChatPanel from "../components/ChatPanel.jsx";
import FriendsPanel from "../components/FriendsPanel.jsx";
import AppShell from "../components/layout/AppShell.jsx";
import { useAuth } from "../context/AuthContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import GameRoomPage from "./GameRoomPage.jsx";

const emptyRoom = { name: "", rows: 10, columns: 10 };

// Polling cadence keeps the lobby, incoming requests and stats feeling live
// even when the SignalR hub is unavailable.
const POLL_INTERVAL_MS = 2000;

export default function DashboardPage() {
  const { api, user } = useAuth();
  const [profile, setProfile] = useState(null);
  const [stats, setStats] = useState(null);
  const [friends, setFriends] = useState([]);
  const [incomingRequests, setIncomingRequests] = useState([]);
  const [sentRequests, setSentRequests] = useState([]);
  const [gameRequests, setGameRequests] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [roomForm, setRoomForm] = useState(emptyRoom);
  const [selectedRoomId, setSelectedRoomId] = useState(null);
  const [selectedFriendId, setSelectedFriendId] = useState(null);
  const [searchResults, setSearchResults] = useState([]);
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);

  const selectedRoomName = useMemo(
    () => rooms.find((room) => room.id === selectedRoomId)?.name,
    [rooms, selectedRoomId]
  );

  const refreshDashboard = useCallback(async () => {
    if (!user?.userId) return;
    const safe = (promise, fallback) => promise.catch(() => fallback);

    const [
      profileData,
      statsData,
      friendsData,
      incomingData,
      sentData,
      gameRequestData,
      roomData
    ] = await Promise.all([
      safe(api.get(`/api/User/${user.userId}`), null),
      safe(api.get(`/api/User/${user.userId}/stats`), null),
      safe(api.get("/api/Friend/list"), []),
      safe(api.get("/api/Friend/requests"), []),
      safe(api.get("/api/Friend/requests/sent"), []),
      safe(api.get("/api/GameRequest/incoming"), []),
      safe(api.get("/api/GameRoom"), [])
    ]);

    if (profileData) setProfile(profileData);
    if (statsData) setStats(statsData);

    setFriends(friendsData ?? []);
    setIncomingRequests(incomingData ?? []);
    setSentRequests(sentData ?? []);
    setGameRequests(gameRequestData ?? []);
    setRooms(roomData ?? []);

    setSelectedFriendId((current) => {
      if (current && (friendsData ?? []).some((friend) => friend.friendId === current)) {
        return current;
      }
      return friendsData?.[0]?.friendId ?? null;
    });
  }, [api, user?.userId]);

  // First refresh is eager so the dashboard is not empty on first paint.
  useEffect(() => {
    refreshDashboard().catch(() => {});
  }, [refreshDashboard]);
  usePolling(refreshDashboard, POLL_INTERVAL_MS);

  // Auto-dismiss the notice toast so it does not linger after a stale error.
  useEffect(() => {
    if (!notice) return undefined;
    const timer = setTimeout(() => setNotice(""), 3500);
    return () => clearTimeout(timer);
  }, [notice]);

  async function createRoom(event) {
    event.preventDefault();
    setNotice("");

    const rows = Number(roomForm.rows);
    const columns = Number(roomForm.columns);
    if (!roomForm.name.trim()) {
      setNotice("Unesi naziv sobe.");
      return;
    }
    if (!Number.isFinite(rows) || !Number.isFinite(columns) || rows < 5 || columns < 5 || rows > 15 || columns > 15) {
      setNotice("Dimenzije table moraju biti izmedju 5 i 15.");
      return;
    }

    setBusy(true);
    try {
      const data = await api.post("/api/GameRoom", {
        name: roomForm.name.trim(),
        rows,
        columns
      });
      setSelectedRoomId(data.roomId);
      setRoomForm(emptyRoom);
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    } finally {
      setBusy(false);
    }
  }

  async function openOrRequestRoom(roomId) {
    try {
      const room = await api.get(`/api/GameRoom/${roomId}`);
      const currentPlayer = (room.players ?? []).find((player) => player.userId === user.userId);
      if (currentPlayer) {
        setSelectedRoomId(roomId);
        return;
      }
      if (room.isStarted) {
      setSelectedRoomId(roomId);
      return;
    }
      await api.post(`/api/GameRequest/join/${roomId}`, {});
      setNotice("Zahtev za ulazak je poslat hostu.");
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function searchUsers(query) {
    if (!query.trim()) {
      setSearchResults([]);
      return;
    }
    try {
      const results = await api.get(`/api/User/search/${encodeURIComponent(query.trim())}`);
      const me = user.userId;
      const friendIds = new Set(friends.map((friend) => friend.friendId));
      const sentIds = new Set(sentRequests.map((request) => request.recipientId));
      const incomingIds = new Set(incomingRequests.map((request) => request.senderId));

      setSearchResults(
        (results ?? [])
          .filter((candidate) => candidate.id !== me)
          .map((candidate) => ({
            ...candidate,
            isFriend: friendIds.has(candidate.id),
            alreadyRequested: sentIds.has(candidate.id) || incomingIds.has(candidate.id)
          }))
      );
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function sendFriendRequest(recipientId) {
    try {
      await api.post(`/api/Friend/request/${recipientId}`, {});
      setNotice("Zahtev za prijateljstvo je poslat.");
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function acceptFriend(requestId) {
    try {
      await api.post(`/api/Friend/request/${requestId}/accept`, {});
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function cancelFriendRequest(requestId) {
    try {
      await api.delete(`/api/Friend/request/${requestId}`);
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function removeFriend(friendId) {
    try {
      await api.delete(`/api/Friend/${friendId}`);
      setSelectedFriendId((current) => (current === friendId ? null : current));
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function acceptGameRequest(requestId) {
    try {
      const data = await api.post(`/api/GameRequest/${requestId}/accept`, {});
      if (data?.roomId) setSelectedRoomId(data.roomId);
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  async function declineGameRequest(requestId) {
    try {
      await api.delete(`/api/GameRequest/${requestId}`);
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }

  const dashboardView = (
    <>
      <div className="topbar">
        <div>
          <p className="eyebrow">Kontrolna tabla</p>
          <h1>{profile?.username || user?.username || "Igrac"}</h1>
        </div>
      </div>

      {notice && <p className="notice top">{notice}</p>}

      <div className="dashboard-layout">
        <main className="dashboard-main">
          <section className="panel stats-panel">
            <div className="section-head">
              <div>
                <p className="eyebrow">Profil</p>
                <h2>{profile?.username || user?.username}</h2>
              </div>
              <small>{profile?.email}</small>
            </div>
            <div className="stat-grid">
              <span>
                <strong>{stats?.gamesWon ?? profile?.gamesWon ?? 0}</strong>
                <small>Pobede</small>
              </span>
              <span>
                <strong>{stats?.gamesLost ?? profile?.gamesLost ?? 0}</strong>
                <small>Porazi</small>
              </span>
              <span>
                <strong>{stats?.winRate ?? 0}%</strong>
                <small>Win rate</small>
              </span>
            </div>
          </section>

          <section className="panel">
            <div className="section-head">
              <div>
                <p className="eyebrow">GameRoom</p>
                <h2>Kreiraj sobu</h2>
              </div>
            </div>
            <form className="stack compact-gap" onSubmit={createRoom}>
              <label>
                Naziv sobe
                <input
                  value={roomForm.name}
                  onChange={(event) =>
                    setRoomForm((current) => ({ ...current, name: event.target.value }))
                  }
                  placeholder="npr. Brza partija"
                />
              </label>
              <div className="inline-fields">
                <label>
                  Redovi
                  <input
                    max="15"
                    min="5"
                    type="number"
                    value={roomForm.rows}
                    onChange={(event) =>
                      setRoomForm((current) => ({ ...current, rows: event.target.value }))
                    }
                  />
                </label>
                <label>
                  Kolone
                  <input
                    max="15"
                    min="5"
                    type="number"
                    value={roomForm.columns}
                    onChange={(event) =>
                      setRoomForm((current) => ({ ...current, columns: event.target.value }))
                    }
                  />
                </label>
              </div>
              <button className="primary" disabled={busy}>
                {busy ? "Kreiram..." : "Kreiraj sobu"}
              </button>
            </form>
          </section>

          <section className="panel">
            <div className="section-head">
              <div>
                <p className="eyebrow">Aktivne sobe</p>
                <h2>Dokumenti za igru</h2>
              </div>
              <small>{rooms.length}</small>
            </div>
            <div className="room-list">
              {rooms.length === 0 && (
                <p className="muted">Nema aktivnih soba. Kreiraj prvu iznad.</p>
              )}
              {rooms.map((room) => (
                <button className="room-item" key={room.id} onClick={() => openOrRequestRoom(room.id)}>
                  <span>
                    <strong>{room.name}</strong>
                    <small>
                      {room.isStarted ? "u toku" : "cekaonica"} • {room.playerCount ?? 0} igraca
                    </small>
                  </span>
                  <span className="room-item-action">
                    {room.isStarted ? "Gledaj" : "Udji"}
                  </span>
                </button>
              ))}
            </div>
          </section>

          <section className="panel">
            <div className="section-head">
              <div>
                <p className="eyebrow">Pozivi</p>
                <h2>Pozivi za igru</h2>
              </div>
              <small>{gameRequests.length}</small>
            </div>
            <div className="mini-list">
              {gameRequests.length === 0 && (
                <p className="muted">Nema dolaznih poziva.</p>
              )}
              {gameRequests.map((request) => (
                <div className="list-row stacked" key={request.id}>
                  <span>
                    <strong>{request.senderUsername || `Korisnik ${request.senderId}`}</strong>
                    <small>
                      {request.isJoinRequest
                        ? `Zeli da udje u sobu ${request.roomName || request.gameRoomId}`
                        : `Te poziva u sobu ${request.roomName || request.gameRoomId}`}
                    </small>
                  </span>
                  <span className="button-pair">
                    <button onClick={() => acceptGameRequest(request.id)}>Prihvati</button>
                    <button onClick={() => declineGameRequest(request.id)}>Odbij</button>
                  </span>
                </div>
              ))}
            </div>
          </section>
        </main>

        <FriendsPanel
          friends={friends}
          incomingRequests={incomingRequests}
          onAcceptFriend={acceptFriend}
          onCancelFriendRequest={cancelFriendRequest}
          onRemoveFriend={removeFriend}
          onSearchUsers={searchUsers}
          onSelectFriend={setSelectedFriendId}
          onSendFriendRequest={sendFriendRequest}
          searchResults={searchResults}
          selectedFriendId={selectedFriendId}
          sentRequests={sentRequests}
        />

      </div>
    </>
  );

  const roomView = selectedRoomId ? (
    <GameRoomPage
      friends={friends}
      gameRequests={gameRequests}
      onAcceptGameRequest={acceptGameRequest}
      onCloseRoom={() => setSelectedRoomId(null)}
      onDeclineGameRequest={declineGameRequest}
      onRefreshDashboard={refreshDashboard}
      roomId={selectedRoomId}
    />
  ) : null;

  const chatPanelNode = (
    <ChatPanel
      friends={friends}
      onSelectFriend={setSelectedFriendId}
      selectedFriendId={selectedFriendId}
    />
  );

  return (
    <AppShell
      chatPanel={chatPanelNode}
      currentView={selectedRoomId ? "room" : "dashboard"}
      gameRequestCount={gameRequests.length}
      onGoHome={() => setSelectedRoomId(null)}
      selectedRoomName={selectedRoomName}
    >
      {selectedRoomId ? roomView : dashboardView}
    </AppShell>
  );
}
