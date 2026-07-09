import React from "react";

export default function Lobby({
  canStart,
  friends,
  gameRequests,
  isHost,
  onAcceptGameRequest,
  onDeclineGameRequest,
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
  const minPlayers = room?.minPlayers ?? 2;

  return (
    <div className="panel lobby-panel">
      <div className="section-head">
        <div>
          <p className="eyebrow">{room?.isStarted ? "Igra u toku" : "Cekaonica"}</p>
          <h2>{room?.name || "Soba"}</h2>
        </div>
        <button className="ghost slim" onClick={onLeaveRoom}>Izadji</button>
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
            return (
              <div className="list-row" key={player.id}>
                <span>
                  <strong>
                    {username}
                    {player.isHost ? " (host)" : ""}
                  </strong>
                  <small>
                    pozicija {position}
                    {disconnected ? " • diskonektovan, cekamo povratak" : ""}
                  </small>
                </span>
              </div>
            );
          })}
        </div>
      </section>

      {!room?.isStarted && isHost && (
        <>
          <section className="mini-section">
            <div className="section-head small-head">
              <h3>Pozovi prijatelja</h3>
              <small>{friends.length}</small>
            </div>
            <div className="mini-list">
              {friends.length === 0 && (
                <p className="muted">Dodaj prijatelje da bi ih pozvao u igru.</p>
              )}
              {friends.map((friend) => (
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
                      {request.isJoinRequest ? "zeli da udje" : "poziv za sobu"}
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

          <button
            className="primary"
            disabled={!canStart}
            onClick={onStartGame}
          >
            Zapocni igru
          </button>
        </>
      )}

      {!isHost && !room?.isStarted && (
        <p className="muted">
          Read-only mod: gledas kako host postavlja zmije i merdevine.
        </p>
      )}
    </div>
  );
}