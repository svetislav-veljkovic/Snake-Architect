import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useCallback, useEffect, useMemo, useState } from "react";
import React from "react";
import ChatPanel from "../components/ChatPanel.jsx";
import FriendsPanel from "../components/FriendsPanel.jsx";
import AppShell from "../components/layout/AppShell.jsx";
import { useAuth } from "../context/AuthContext.jsx";
import { HUB_URL } from "../utils/api.js";
import { usePolling } from "../hooks/usePolling.js";
import GameRoomPage from "./GameRoomPage.jsx";
import ProfilePage from "./ProfilePage.jsx";
const emptyRoom = { name: "" };
const POLL_INTERVAL_MS = 2000;
const PENDING_KEY_PREFIX = "snakeArchitect.pendingJoin.";
const INTENTIONAL_LEAVE_PREFIX = "snakeArchitect.intentionalLeave.";
function MatchHistory({ history }) {
  if (!history || history.length === 0) {
    return <p className="muted match-history-empty">Jos nema zavrsenih partija.</p>;
  }
  return (
    <div className="match-history match-history-list">
      {history.map((entry) => (
        <article
          className={"match-row " + (entry.isWin ? "win" : "loss")}
          key={entry.roomId + "-" + entry.playedAt}
        >
          <span className={"match-badge " + (entry.isWin ? "win" : "loss")}>
            {entry.isWin ? "W" : "L"}
          </span>
          <span>
            <strong>{entry.roomName || "Partija"}</strong>
            <small>Igraci: {(entry.playerUsernames ?? []).join(", ") || "n/a"}</small>
            <small className="match-meta-line">
              <span>Pobednik: {entry.winnerUsername || "n/a"}</span>
              <time dateTime={entry.playedAt}>{new Date(entry.playedAt).toLocaleString()}</time>
            </small>
          </span>
        </article>
      ))}
    </div>
  );
}
function readPendingJoinIds(userId) {
  if (!userId) return new Set();
  try {
    const raw = localStorage.getItem(PENDING_KEY_PREFIX + userId);
    return raw ? new Set(JSON.parse(raw)) : new Set();
  } catch {
    return new Set();
  }
}
function intentionalLeaveKey(userId, roomId) {
  return `${INTENTIONAL_LEAVE_PREFIX}${userId}.${roomId}`;
}
function hasIntentionallyLeft(userId, roomId) {
  if (!userId || !roomId) return false;
  try {
    return localStorage.getItem(intentionalLeaveKey(userId, roomId)) === "1";
  } catch {
    return false;
  }
}
function clearIntentionalLeave(userId, roomId) {
  if (!userId || !roomId) return;
  try {
    localStorage.removeItem(intentionalLeaveKey(userId, roomId));
  } catch {}
}
export default function DashboardPage() {
  const { api, token, user, refreshProfile } = useAuth();
  const [profile, setProfile] = useState(null);
  const [stats, setStats] = useState(null);
  const [friends, setFriends] = useState([]);
  const [incomingRequests, setIncomingRequests] = useState([]);
  const [sentRequests, setSentRequests] = useState([]);
  const [gameRequests, setGameRequests] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [roomForm, setRoomForm] = useState(emptyRoom);
  const [minPlayers, setMinPlayers] = useState(2);
  const [selectedRoomId, setSelectedRoomId] = useState(null);
  const [showProfile, setShowProfile] = useState(false);
  const [selectedFriendId, setSelectedFriendId] = useState(null);
  const [searchResults, setSearchResults] = useState([]);
  const [matchHistory, setMatchHistory] = useState([]);
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [pendingJoinRoomIds, setPendingJoinRoomIds] = useState(() =>
    readPendingJoinIds(user?.userId)
  );
  const [onlineUserIds, setOnlineUserIds] = useState(() => new Set());
  const selectedRoomName = useMemo(
    () => rooms.find((room) => room.id === selectedRoomId)?.name,
    [rooms, selectedRoomId]
  );
  const returnableRooms = useMemo(
    () =>
      rooms.filter((room) =>
        (room.isStarted || room.boardConfirmed) &&
        room.isMember &&
        !hasIntentionallyLeft(user?.userId, room.id)
      ),
    [rooms, user?.userId]
  );
  const availableRooms = useMemo(
    () =>
      returnableRooms.length > 0
        ? []
        : rooms.filter((room) => room.isStarted || room.boardConfirmed),
    [returnableRooms.length, rooms]
  );
  const playableRooms = useMemo(
    () =>
      availableRooms.filter((room) => {
        const hasOpenSlot = (room.playerCount ?? 0) < (room.minPlayers ?? 2);
        return hasOpenSlot;
      }),
    [availableRooms]
  );
  const watchableRooms = useMemo(
    () =>
      availableRooms.filter((room) => {
        const hasOpenSlot = (room.playerCount ?? 0) < (room.minPlayers ?? 2);
        return room.isStarted && !hasOpenSlot && !room.isMember;
      }),
    [availableRooms]
  );
  function persistPendingJoinIds(nextSet) {
    if (!user?.userId) return;
    try {
      localStorage.setItem(PENDING_KEY_PREFIX + user.userId, JSON.stringify([...nextSet]));
    } catch {}
  }
  useEffect(() => {
    refreshProfile().catch(() => {});
  }, []);
  useEffect(() => {
    if (!token) return undefined;
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    connection.on("UserStatusChanged", (userId, isOnline) => {
      setOnlineUserIds((current) => {
        const next = new Set(current);
        if (isOnline) next.add(userId);
        else next.delete(userId);
        return next;
      });
    });
    connection.on("JoinedGameRoom", (roomId) => {
      if (!roomId) return;
      setShowProfile(false);
      setPendingJoinRoomIds(new Set());
      try {
        localStorage.setItem(PENDING_KEY_PREFIX + user.userId, JSON.stringify([]));
      } catch {}
      setSelectedRoomId(roomId);
      setNotice("Host je prihvatio tvoj zahtev. Ulazis u cekaonicu.");
    });
    let cancelled = false;
    async function start() {
      try {
        await connection.start();
        if (cancelled) return;
        if (user?.username) {
          await connection.invoke("JoinGroup", user.username);
        }
        const ids = await connection.invoke("GetOnlineUsers");
        if (!cancelled) setOnlineUserIds(new Set(ids ?? []));
      } catch (e) {}
    }
    start();
    return () => {
      cancelled = true;
      connection.stop().catch(() => {});
    };
  }, [token, user?.userId, user?.username]);
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
      roomData,
      historyData
    ] = await Promise.all([
      safe(api.get(`/api/User/${user.userId}`), null),
      safe(api.get(`/api/User/${user.userId}/stats`), null),
      safe(api.get("/api/Friend/list"), []),
      safe(api.get("/api/Friend/requests"), []),
      safe(api.get("/api/Friend/requests/sent"), []),
      safe(api.get("/api/GameRequest/incoming"), []),
      safe(api.get("/api/GameRoom"), []),
      safe(api.get(`/api/User/${user.userId}/history?limit=5`), [])
    ]);
    if (profileData) setProfile(profileData);
    if (statsData) setStats(statsData);
    setFriends(friendsData ?? []);
    setIncomingRequests(incomingData ?? []);
    setSentRequests(sentData ?? []);
    setGameRequests(gameRequestData ?? []);
    setRooms(roomData ?? []);
    setMatchHistory(historyData ?? []);
    const freshFriendIds = new Set((friendsData ?? []).map((friend) => friend.friendId));
    const freshSentIds = new Set((sentData ?? []).map((request) => request.recipientId));
    const freshIncomingIds = new Set((incomingData ?? []).map((request) => request.senderId));
    setSearchResults((current) =>
      current.map((candidate) => {
        const candidateId = candidate.id ?? candidate.userId;
        return {
          ...candidate,
          isFriend: freshFriendIds.has(candidateId),
          alreadyRequested:
            !freshFriendIds.has(candidateId) &&
            (freshSentIds.has(candidateId) || freshIncomingIds.has(candidateId))
        };
      })
    );
    setSelectedFriendId((current) => {
      if (current && (friendsData ?? []).some((friend) => friend.friendId === current)) {
        return current;
      }
      return null;
    });
    if (pendingJoinRoomIds.size > 0) {
      const stillPending = new Set(pendingJoinRoomIds);
      let joinedRoomId = null;
      (roomData ?? []).forEach((room) => {
        if (stillPending.has(room.id) && room.isMember) {
          stillPending.delete(room.id);
          joinedRoomId = room.id;
        }
      });
      if (joinedRoomId !== null) {
        persistPendingJoinIds(new Set());
        setPendingJoinRoomIds(new Set());
        setSelectedRoomId(joinedRoomId);
        setNotice("Host je prihvatio tvoj zahtev. Pridruzio/la si se sobi.");
      }
    }
  }, [api, user?.userId, pendingJoinRoomIds]);
  useEffect(() => {
    refreshDashboard().catch(() => {});
  }, [refreshDashboard]);
  usePolling(refreshDashboard, POLL_INTERVAL_MS);
  useEffect(() => {
    if (!notice) return undefined;
    const timer = setTimeout(() => setNotice(""), 3500);
    return () => clearTimeout(timer);
  }, [notice]);
  async function createRoom(event) {
    event.preventDefault();
    setNotice("");
    if (!roomForm.name.trim()) {
      setNotice("Unesi naziv sobe.");
      return;
    }
    setBusy(true);
    try {
      const data = await api.post("/api/GameRoom", {
        name: roomForm.name.trim(),
        minPlayers
      });
      setSelectedRoomId(data.roomId);
      setRoomForm(emptyRoom);
      setMinPlayers(2);
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    } finally {
      setBusy(false);
    }
  }
  async function openOrRequestRoom(roomId) {
    try {
      clearIntentionalLeave(user?.userId, roomId);
      const room = await api.get(`/api/GameRoom/${roomId}`);
      const currentPlayer = (room.players ?? []).find((player) => player.userId === user.userId);
      if (currentPlayer) {
        setSelectedRoomId(roomId);
        return;
      }
      const visiblePlayerCount = room.players?.length ?? room.playerCount ?? 0;
      const hasOpenReplacementSlot = room.isStarted && visiblePlayerCount < (room.minPlayers ?? 2);
      if (room.isStarted && !hasOpenReplacementSlot) {
        setSelectedRoomId(roomId);
        return;
      }
      if (pendingJoinRoomIds.has(roomId)) {
        return;
      }
      await api.post(`/api/GameRequest/join/${roomId}`, {});
      setNotice("Zahtev za ulazak je poslat hostu.");
      const next = new Set(pendingJoinRoomIds);
      next.add(roomId);
      setPendingJoinRoomIds(next);
      persistPendingJoinIds(next);
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
      setSearchResults((current) =>
        current.map((candidate) =>
          (candidate.id ?? candidate.userId) === recipientId
            ? { ...candidate, alreadyRequested: true }
            : candidate
        )
      );
      await refreshDashboard();
    } catch (error) {
      setNotice(error.message);
    }
  }
  async function acceptFriend(requestId) {
    try {
      const request = incomingRequests.find((item) => item.id === requestId);
      await api.post(`/api/Friend/request/${requestId}/accept`, {});
      if (request?.senderId) {
        setSearchResults((current) =>
          current.map((candidate) =>
            (candidate.id ?? candidate.userId) === request.senderId
              ? { ...candidate, isFriend: true, alreadyRequested: false }
              : candidate
          )
        );
      }
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
      {notice && <p className="notice top">{notice}</p>}
      <div className="dashboard-layout">
        <main className="dashboard-main">
          {returnableRooms.length > 0 && (
            <section className="panel resume-panel">
              <div>
                <p className="eyebrow">
                  {returnableRooms.some((room) => room.isStarted)
                    ? "Partija u toku"
                    : "Cekaonica"}
                </p>
                <h2>
                  {returnableRooms.some((room) => room.isStarted)
                    ? "Vrati se u igru"
                    : "Vrati se u cekaonicu"}
                </h2>
              </div>
              <div className="room-list">
                {returnableRooms.map((room) => (
                  <button
                    className="room-item resume-room"
                    key={room.id}
                    onClick={() => openOrRequestRoom(room.id)}
                  >
                    <span>
                      <strong>{room.name}</strong>
                      <small>
                        {room.isStarted && room.myPlayerIsConnected === false
                          ? "Diskonektovan/a si. Klikni da se vratis u partiju."
                          : room.isMyTurn
                          ? "Ostali igraci cekaju tvoj potez."
                          : room.isStarted
                          ? "Partija je jos u toku."
                          : "Partija samo sto nije pocela."}
                      </small>
                    </span>
                    <span className="room-item-action">
                      {room.isStarted ? "Vrati se" : "Udji"}
                    </span>
                  </button>
                ))}
              </div>
            </section>
          )}
          {returnableRooms.length === 0 && (
          <section className="panel">
            <div className="section-head">
              <div>
                <h2>Kreiraj sobu</h2>
              </div>
            </div>
            <form className="stack compact-gap" onSubmit={createRoom}>
              <div className="inline-fields">
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
                <label>
                  Broj igraca
                  <select
                    value={minPlayers}
                    onChange={(event) => setMinPlayers(Number(event.target.value))}
                  >
                    {[2, 3, 4, 5, 6].map((n) => (
                      <option key={n} value={n}>{n}</option>
                    ))}
                  </select>
                </label>
              </div>
              <button className="primary" disabled={busy}>
                {busy ? "Kreiram..." : "Kreiraj sobu"}
              </button>
            </form>
          </section>
          )}
          {returnableRooms.length === 0 && (
          <section className="panel">
            <div className="section-head">
              <div>
                <h2>Sobe za igranje</h2>
              </div>
              <small>{playableRooms.length}</small>
            </div>
            <div className="room-list">
              {playableRooms.length === 0 && (
                <p className="muted">Nema soba za ulazak kao igrac.</p>
              )}
              {playableRooms.map((room) => {
                const isPending =
                  pendingJoinRoomIds.has(room.id) && !room.isMember && !room.isStarted;
                let actionLabel = "Udji";
                if (isPending) {
                  actionLabel = "Cekam potvrdu";
                } else if (room.isStarted) {
                  const hasOpenReplacementSlot = (room.playerCount ?? 0) < (room.minPlayers ?? 2);
                  actionLabel = room.isMember ? "Udji" : hasOpenReplacementSlot ? "Udji" : "Gledaj";
                }
                return (
                  <button
                    className="room-item"
                    key={room.id}
                    disabled={isPending}
                    onClick={() => openOrRequestRoom(room.id)}
                  >
                    <span>
                      <strong>{room.name}</strong>
                      <small>Host: {room.hostUsername || "n/a"}</small>
                      <small>Igraci {room.playerCount ?? 0}/{room.minPlayers ?? 2}</small>
                    </span>
                    <span className="room-item-action">{actionLabel}</span>
                  </button>
                );
              })}
            </div>
          </section>
          )}
          {returnableRooms.length === 0 && (
          <section className="panel">
            <div className="section-head">
              <div>
                <h2>Sobe za gledanje</h2>
              </div>
              <small>{watchableRooms.length}</small>
            </div>
            <div className="room-list">
              {watchableRooms.length === 0 && (
                <p className="muted">Nema partija za gledanje.</p>
              )}
              {watchableRooms.map((room) => (
                <button
                  className="room-item"
                  key={room.id}
                  onClick={() => openOrRequestRoom(room.id)}
                >
                  <span>
                    <strong>{room.name}</strong>
                    <small>Host: {room.hostUsername || "n/a"}</small>
                    <small>Igraci {room.playerCount ?? 0}/{room.minPlayers ?? 2}</small>
                  </span>
                  <span className="room-item-action">Gledaj</span>
                </button>
              ))}
            </div>
          </section>
          )}
          {returnableRooms.length === 0 && (
          <section className="panel">
            <div className="section-head">
              <div>
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
          )}
        </main>
        <aside className="dashboard-side">
          <section className="panel stats-panel">
            <div className="section-head">
              <div>
                <p className="eyebrow">Statistika</p>
                <h2>{profile?.username || user?.username}</h2>
              </div>
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
            <div className="match-history-block dashboard-history">
              <p className="eyebrow">Poslednjih 5 partija</p>
              <MatchHistory history={matchHistory} />
            </div>
          </section>
        </aside>
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
  const profileView = <ProfilePage onClose={() => setShowProfile(false)} />;
  const chatPanelNode = (
    <ChatPanel
      friends={friends}
      onlineUserIds={onlineUserIds}
      onSelectFriend={setSelectedFriendId}
      selectedFriendId={selectedFriendId}
    />
  );
  const socialPanelNode = (
    <FriendsPanel
      friends={friends}
      incomingRequests={incomingRequests}
      onAcceptFriend={acceptFriend}
      onCancelFriendRequest={cancelFriendRequest}
      onlineUserIds={onlineUserIds}
      onRemoveFriend={removeFriend}
      onSearchUsers={searchUsers}
      onSelectFriend={setSelectedFriendId}
      onSendFriendRequest={sendFriendRequest}
      searchResults={searchResults}
      selectedFriendId={selectedFriendId}
      sentRequests={sentRequests}
    />
  );
  const currentView = showProfile ? "profile" : selectedRoomId ? "room" : "dashboard";
  return (
    <AppShell
      chatPanel={chatPanelNode}
      currentView={currentView}
      gameRequestCount={gameRequests.length}
      onGoHome={() => {
        setShowProfile(false);
        setSelectedRoomId(null);
      }}
      onGoProfile={() => setShowProfile(true)}
      selectedRoomName={selectedRoomName}
      socialPanel={socialPanelNode}
    >
      {showProfile ? profileView : selectedRoomId ? roomView : dashboardView}
    </AppShell>
  );
}
