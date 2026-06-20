import { useState } from "react";
import React from "react";

export default function FriendsPanel({
  friends,
  incomingRequests,
  onAcceptFriend,
  onCancelFriendRequest,
  onRemoveFriend,
  onSearchUsers,
  onSelectFriend,
  onSendFriendRequest,
  searchResults,
  selectedFriendId,
  sentRequests
}) {
  const [query, setQuery] = useState("");

  function handleSearch(event) {
    event.preventDefault();
    onSearchUsers(query);
  }

  return (
    <div className="panel social-panel">
      <div className="section-head">
        <div>
          <p className="eyebrow">Socijalni sloj</p>
          <h2>Prijatelji</h2>
        </div>
      </div>

      <form className="search-row" onSubmit={handleSearch}>
        <input
          placeholder="Pretrazi korisnike po username-u"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        <button className="icon-button" type="submit">Trazi</button>
      </form>

      {searchResults.length > 0 && (
        <div className="mini-list search-results">
          {searchResults.map((candidate) => (
            <div className="list-row" key={candidate.id ?? candidate.userId ?? candidate.username}>
              <span>
                <strong>{candidate.username}</strong>
                <small>{candidate.email || candidate.name || "kandidat"}</small>
              </span>
              <button
                disabled={candidate.isFriend || candidate.alreadyRequested}
                onClick={() => onSendFriendRequest(candidate.id ?? candidate.userId)}
              >
                {candidate.isFriend
                  ? "Prijatelj"
                  : candidate.alreadyRequested
                    ? "Poslato"
                    : "Dodaj"}
              </button>
            </div>
          ))}
        </div>
      )}

      <div className="split-block">
        <section>
          <div className="section-head small-head">
            <h3>Moji prijatelji</h3>
            <small>{friends.length}</small>
          </div>
          <div className="mini-list">
            {friends.length === 0 && (
              <p className="muted">Jos nema prijatelja. Pozovi nekoga iz pretrage.</p>
            )}
            {friends.map((friend) => (
              <div
                className={`list-row selectable ${
                  selectedFriendId === friend.friendId ? "active" : ""
                }`}
                key={friend.friendId}
              >
                <button className="friend-pick" onClick={() => onSelectFriend(friend.friendId)}>
                  <strong>{friend.friendUsername || `Korisnik ${friend.friendId}`}</strong>
                  <small>{friend.friendName || "klikni za cet"}</small>
                </button>
                <button onClick={() => onRemoveFriend(friend.friendId)}>Ukloni</button>
              </div>
            ))}
          </div>
        </section>

        <section>
          <div className="section-head small-head">
            <h3>Zahtevi</h3>
            <small>{incomingRequests.length + sentRequests.length}</small>
          </div>
          <div className="mini-list">
            {incomingRequests.length === 0 && sentRequests.length === 0 && (
              <p className="muted">Nema aktivnih zahteva.</p>
            )}
            {incomingRequests.map((request) => (
              <div className="list-row stacked" key={request.id}>
                <span>
                  <strong>{request.senderUsername || `Korisnik ${request.senderId}`}</strong>
                  <small>salje zahtev za prijateljstvo</small>
                </span>
                <span className="button-pair">
                  <button onClick={() => onAcceptFriend(request.id)}>Prihvati</button>
                  <button onClick={() => onCancelFriendRequest(request.id)}>Odbij</button>
                </span>
              </div>
            ))}
            {sentRequests.map((request) => (
              <div className="list-row" key={request.id}>
                <span>
                  <strong>{request.recipientUsername || `Korisnik ${request.recipientId}`}</strong>
                  <small>poslat zahtev</small>
                </span>
                <button onClick={() => onCancelFriendRequest(request.id)}>Povuci</button>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
