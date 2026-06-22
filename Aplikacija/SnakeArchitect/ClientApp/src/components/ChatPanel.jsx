import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import React from "react";
import { useAuth } from "../context/AuthContext.jsx";
import { usePolling } from "../hooks/usePolling.js";

const POLL_INTERVAL_MS = 2000;

export default function ChatPanel({ friends, roomName, selectedFriendId, onSelectFriend }) {
  const { api, user } = useAuth();
  const [messages, setMessages] = useState([]);
  const [draft, setDraft] = useState("");
  const [error, setError] = useState("");
  const logRef = useRef(null);

  const selectedFriend = useMemo(
    () => friends.find((friend) => friend.friendId === selectedFriendId),
    [friends, selectedFriendId]
  );

  const loadConversation = useCallback(async () => {
    if (!selectedFriendId || !user?.userId) {
      setMessages([]);
      return;
    }
    try {
      const data = await api.get(`/api/Chat/conversation/${selectedFriendId}`);
      setMessages(data ?? []);
      setError("");
    } catch (err) {
      setError(err.message);
    }
  }, [api, selectedFriendId, user?.userId]);

  useEffect(() => {
    setMessages([]);
    loadConversation().catch(() => {});
  }, [loadConversation]);
  usePolling(loadConversation, POLL_INTERVAL_MS);

  useEffect(() => {
    if (!logRef.current) return;
    logRef.current.scrollTo({ top: logRef.current.scrollHeight, behavior: "smooth" });
  }, [messages.length]);

  async function sendMessage(event) {
    event.preventDefault();
    if (!selectedFriendId || !draft.trim()) return;
    try {
      await api.post("/api/Chat/send", {
        senderId: user.userId,
        recipientId: selectedFriendId,
        content: draft.trim(),
        sentAt: new Date().toISOString()
      });
      setDraft("");
      await loadConversation();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <aside className="panel chat-panel">
      <div className="section-head">
        <div>
          <p className="eyebrow">{roomName ? "Cet u sobi" : "Privatni cet"}</p>
          <h2>{selectedFriend ? selectedFriend.friendUsername : "Izaberi prijatelja"}</h2>
        </div>
      </div>

      <div className="chat-friends">
        {friends.length === 0 && (
          <p className="muted chat-empty-friends">Dodaj prijatelje da bi zapoceo razgovor.</p>
        )}
        {friends.map((friend) => (
          <button
            className={selectedFriendId === friend.friendId ? "active" : ""}
            key={friend.friendId}
            onClick={() => onSelectFriend(friend.friendId)}
          >
            {friend.friendUsername || `Korisnik ${friend.friendId}`}
          </button>
        ))}
      </div>

      <div className="chat-log" ref={logRef}>
        {!selectedFriend && <p className="muted">Izaberi prijatelja sa leve strane.</p>}
        {selectedFriend && messages.length === 0 && (
          <p className="muted">Nema poruka. Posalji prvu.</p>
        )}
        {selectedFriend &&
          messages.map((message) => (
            <p className={message.isOwn ? "own" : ""} key={message.id}>
              <span>{message.content}</span>
              <small>
                {message.isOwn ? "Ti" : selectedFriend.friendUsername} •{" "}
                {new Date(message.sentAt).toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit"
                })}
              </small>
            </p>
          ))}
      </div>

      <form className="chat-form" onSubmit={sendMessage}>
        <input
          disabled={!selectedFriend}
          placeholder={selectedFriend ? "Napisi poruku..." : "Izaberi prijatelja"}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
        />
        <button disabled={!selectedFriend || !draft.trim()} type="submit">Posalji</button>
      </form>

      {error && <p className="notice compact-notice">{error}</p>}
    </aside>
  );
}
