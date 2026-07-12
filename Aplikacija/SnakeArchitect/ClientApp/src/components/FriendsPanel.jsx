import { useState } from "react";
import React from "react";

function StatusDot({ online }) {
  return (
    <span
      className={"status-dot " + (online ? "online" : "offline")}
      title={online ? "Online" : "Offline"}
    />
  );
}

export default function FriendsPanel({
  friends,
  incomingRequests,
  onAcceptFriend,
  onCancelFriendRequest,
  onlineUserIds,
  onRemoveFriend,
  onSearchUsers,
  onSelectFriend,
  onSendFriendRequest,
  searchResults,
  selectedFriendId,
  sentRequests
}) {
  const [query, setQuery] = useState("");
  const [activeTab, setActiveTab] = useState("search");
  const requestCount = incomingRequests.length + sentRequests.length;

  function isOnline(userId) {
    return Boolean(onlineUserIds && onlineUserIds.has(userId));
  }

  function handleSearch(event) {
    event.preventDefault();
    onSearchUsers(query);
  }

  return (
    <div className="panel social-panel social-tabs-panel">
      <div className="section-head">
        <div>
          <h2>Prijatelji</h2>
        </div>
      </div>

      <div className="social-tabs" role="tablist" aria-label="Prijatelji">
        <button
          className={activeTab === "search" ? "active" : ""}
          onClick={() => setActiveTab("search")}
          type="button"
        >
          Pretrazi
        </button>
        <button
          className={activeTab === "friends" ? "active" : ""}
          onClick={() => setActiveTab("friends")}
          type="button"
        >
          Moji prijatelji
        </button>
        <button
          className={activeTab === "requests" ? "active" : ""}
          onClick={() => setActiveTab("requests")}
          type="button"
        >
          Zahtevi
          {requestCount > 0 && <span className="tab-badge">{requestCount}</span>}
        </button>
      </div>

      <div className="social-tab-content">
        {activeTab === "search" && (
          <>
            <form className="search-row" onSubmit={handleSearch}>
              <input
                placeholder="Unesi username"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
              />
              <button className="icon-button" type="submit">Trazi</button>
            </form>

            <div className="mini-list search-results social-scroll-list">
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
          </>
        )}

        {activeTab === "friends" && (
          <div className="mini-list social-scroll-list">
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
                  <strong>
                    <StatusDot online={isOnline(friend.friendId)} />
                    {friend.friendUsername || `Korisnik ${friend.friendId}`}
                  </strong>
                  <small>{friend.friendName || "klikni za cet"}</small>
                </button>
                <button onClick={() => onRemoveFriend(friend.friendId)}>Ukloni</button>
              </div>
            ))}
          </div>
        )}

        {activeTab === "requests" && (
          <div className="mini-list social-scroll-list">
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
        )}
      </div>
    </div>
  );
}
