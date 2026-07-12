import { useState } from "react";
import React from "react";

export default function Lobby({
  canStart,
  friends,
  gameRequests,
  isHost,
  onAcceptGameRequest,
  onDeclineGameRequest,
  onDeleteRoom,
  onInviteFriend,
  onLeaveRoom,
  onStartGame,
  players,
  room,
  userNames
}) {
  const roomRequests = (gameRequests ?? []).filter(
    (request) => request.gameRoomId === room?.id
  );
  const playerUserIds = new Set((players ?? []).map((player) => player.userId));
  const invitableFriends = (friends ?? []).filter((friend) => !playerUserIds.has(friend.friendId));
  const minPlayers = room?.minPlayers ?? 2;
  const hasOpenSlot = (players ?? []).length < minPlayers;
  const canManagePlayers = isHost && (!room?.isStarted || hasOpenSlot);

  // FIX: zamenjen ruzan native window.confirm() stilizovanim modalom koji
  // se uklapa u ostatak aplikacije (isti "overlay" pattern kao
  // winner-overlay u GameRoomPage.jsx).
  const [confirmOpen, setConfirmOpen] = useState(false);

  function handleDeleteRoom() {
    if (!onDeleteRoom) return;
    setConfirmOpen(true);
  }

  function confirmDelete() {
    setConfirmOpen(false);
    onDeleteRoom();
  }

  return (
    <div className="panel lobby-panel">
      <div className="section-head">
        <div>
          <p className="eyebrow">{room?.isStarted ? "Igra u toku" : "Cekaonica"}</p>
          <h2>{room?.name || "Soba"}</h2>
        </div>
        <span className="button-pair">
          {isHost && onDeleteRoom && (
            <button className="ghost slim" onClick={handleDeleteRoom}>
              {room?.isStarted ? "Prekini partiju" : "Obrisi sobu"}
            </button>
          )}
        </span>
      </div>

      <section className="mini-section">
        <div className="section-head small-head">
          <h3>Igraci</h3>
          <small>{players.length}/{minPlayers}</small>
        </div>
        {!room?.isStarted && players.length < minPlayers && (
          <p className="muted">
            Potrebno je jos {minPlayers - players.length} igraca da bi partija mogla da pocne.
          </p>
        )}
        <div className="mini-list">
          {players.length === 0 && (
            <p className="muted">Jos niko nije usao. Pozovi prijatelje.</p>
          )}
          {players.map((player) => {
            const username = userNames[player.userId] || ("Korisnik " + player.userId);
            const position = Math.max(1, player.currentPosition || 1);
            const disconnected = player.isConnected === false;
            const statusLabel = disconnected ? "Diskonektovan" : "Aktivan";
            return (
              <div className={"list-row player-list-row " + (disconnected ? "disconnected" : "connected")} key={player.id}>
                <span>
                  <strong>
                    <span
                      className={"status-dot " + (disconnected ? "disconnected" : "online")}
                      title={statusLabel}
                    />
                    {username}
                    {player.isHost ? " (host)" : ""}
                  </strong>
                  <small>
                    Pozicija {position}
                    {disconnected ? " • diskonektovan, cekamo povratak" : ""}
                  </small>
                </span>
              </div>
            );
          })}
        </div>
      </section>

      {canManagePlayers && (
        <>
          <section className="mini-section">
            <div className="section-head small-head">
              <h3>Pozovi prijatelja</h3>
              <small>{invitableFriends.length}</small>
            </div>
            <div className="mini-list">
              {friends.length === 0 && (
                <p className="muted">Dodaj prijatelje da bi ih pozvao u igru.</p>
              )}
              {friends.length > 0 && invitableFriends.length === 0 && (
                <p className="muted">Svi prijatelji iz liste su vec u ovoj sobi.</p>
              )}
              {invitableFriends.map((friend) => (
                <div className="list-row" key={friend.friendId}>
                  <span>
                    <strong>{friend.friendUsername || ("Korisnik " + friend.friendId)}</strong>
                    <small>{friend.friendName || "prijatelj"}</small>
                  </span>
                  <button onClick={() => onInviteFriend(friend.friendId)}>Pozovi</button>
                </div>
              ))}
            </div>
          </section>

          <section className="mini-section">
            <div className="section-head small-head">
              <h3>Zahtevi za ulazak</h3>
              <small>{roomRequests.length}</small>
            </div>
            <div className="mini-list">
              {roomRequests.length === 0 && (
                <p className="muted">Nema zahteva za ovu sobu.</p>
              )}
              {roomRequests.map((request) => (
                <div className="list-row stacked" key={request.id}>
                  <span>
                    <strong>{request.senderUsername || ("Korisnik " + request.senderId)}</strong>
                    <small>
                      {request.isJoinRequest ? "Zeli da udje" : "Poziv za sobu"}
                    </small>
                  </span>
                  <span className="button-pair">
                    <button onClick={() => onAcceptGameRequest(request.id)}>Prihvati</button>
                    <button onClick={() => onDeclineGameRequest(request.id)}>Odbij</button>
                  </span>
                </div>
              ))}
            </div>
          </section>

          {!room?.isStarted && (
            <button
              className="primary"
              disabled={!canStart}
              onClick={onStartGame}
            >
              Zapocni igru
            </button>
          )}
        </>
      )}

      {confirmOpen && (
        <div className="confirm-overlay" role="dialog" aria-modal="true">
          <div className="confirm-modal">
            <p className="eyebrow">Potvrda</p>
            <h2>{room?.isStarted ? "Prekini partiju?" : "Obrisi sobu?"}</h2>
            <p>
              {room?.isStarted
                ? "Sigurno zelis da prekines i obrises ovu partiju? Ova akcija je trajna i ne moze se opozvati."
                : "Sigurno zelis da obrises ovu sobu? Ova akcija je trajna."}
            </p>
            <span className="button-pair">
              <button className="ghost" onClick={() => setConfirmOpen(false)}>
                Odustani
              </button>
              <button className="danger" onClick={confirmDelete}>
                {room?.isStarted ? "Prekini partiju" : "Obrisi sobu"}
              </button>
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
