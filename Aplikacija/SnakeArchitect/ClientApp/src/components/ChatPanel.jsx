import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import React from "react";
import { useAuth } from "../context/AuthContext.jsx";
import { usePolling } from "../hooks/usePolling.js";

const POLL_INTERVAL_MS = 2000;
const CHAT_SEEN_PREFIX = "snakeArchitect.chatSeen.";

function readSeenMessageTimes(userId) {
  if (!userId) return new Map();
  try {
    const raw = localStorage.getItem(CHAT_SEEN_PREFIX + userId);
    const parsed = raw ? JSON.parse(raw) : {};
    return new Map(Object.entries(parsed).map(([friendId, seenAt]) => [Number(friendId), seenAt]));
  } catch {
    return new Map();
  }
}

function persistSeenMessageTimes(userId, seenMap) {
  if (!userId) return;
  try {
    localStorage.setItem(CHAT_SEEN_PREFIX + userId, JSON.stringify(Object.fromEntries(seenMap)));
  } catch {
    /* ignore */
  }
}

function StatusDot({ online }) {
  return (
    <span
      className={"status-dot " + (online ? "online" : "offline")}
      title={online ? "Online" : "Offline"}
    />
  );
}

export default function ChatPanel({ friends, onlineUserIds, roomName, selectedFriendId, onSelectFriend }) {
  const { api, user } = useAuth();
  const [messages, setMessages] = useState([]);
  const [draft, setDraft] = useState("");
  const [error, setError] = useState("");
  const [unreadByFriendId, setUnreadByFriendId] = useState({});
  const logRef = useRef(null);
  const seenMessageTimes = useRef(new Map());
  const inboxInitialized = useRef(false);

  const selectedFriend = useMemo(
    () => friends.find((friend) => friend.friendId === selectedFriendId),
    [friends, selectedFriendId]
  );

  function isOnline(userId) {
    return Boolean(onlineUserIds && onlineUserIds.has(userId));
  }

  const markFriendSeen = useCallback((friendId) => {
    if (!friendId) return;
    seenMessageTimes.current.set(friendId, Date.now());
    persistSeenMessageTimes(user?.userId, seenMessageTimes.current);
    setUnreadByFriendId((current) => {
      if (!current[friendId]) return current;
      const next = { ...current };
      delete next[friendId];
      return next;
    });
  }, [user?.userId]);

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

  const loadInbox = useCallback(async () => {
    if (!user?.userId) {
      inboxInitialized.current = false;
      seenMessageTimes.current.clear();
      setUnreadByFriendId({});
      return;
    }

    try {
      if (!inboxInitialized.current) {
        seenMessageTimes.current = readSeenMessageTimes(user.userId);
      }

      const inbox = await api.get("/api/Chat/inbox");
      setUnreadByFriendId((current) => {
        const next = { ...current };
        let seenChanged = false;

        (inbox ?? []).forEach((item) => {
          const friendId = item.otherUserId;
          if (!friendId) return;

          const lastMessageAt = Date.parse(item.lastMessageAt);
          const safeLastMessageAt = Number.isNaN(lastMessageAt) ? Date.now() : lastMessageAt;
          const seenAt = seenMessageTimes.current.get(friendId);

          if (item.isLastMessageOwn || friendId === selectedFriendId) {
            seenMessageTimes.current.set(friendId, safeLastMessageAt);
            seenChanged = true;
            delete next[friendId];
            return;
          }

          if (!inboxInitialized.current && seenAt === undefined) {
            seenMessageTimes.current.set(friendId, safeLastMessageAt);
            seenChanged = true;
            return;
          }

          if (seenAt !== undefined && safeLastMessageAt <= seenAt) {
            delete next[friendId];
            return;
          }

          next[friendId] = 1;
        });

        inboxInitialized.current = true;
        if (seenChanged) persistSeenMessageTimes(user.userId, seenMessageTimes.current);
        return next;
      });
    } catch (err) {
      setError(err.message);
    }
  }, [api, selectedFriendId, user?.userId]);

  useEffect(() => {
    markFriendSeen(selectedFriendId);
  }, [markFriendSeen, selectedFriendId]);

  usePolling(loadInbox, POLL_INTERVAL_MS);

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
      markFriendSeen(selectedFriendId);
      await loadConversation();
      await loadInbox();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <aside className="panel chat-panel">
      <div className="section-head">
        <div>
          <h2>Chat</h2>
        </div>
      </div>

      <div className="chat-friends">
        {friends.length === 0 && (
          <p className="muted chat-empty-friends">Dodaj prijatelje da bi zapoceo razgovor.</p>
        )}
        {friends.map((friend) => (
          <button
            className={[
              selectedFriendId === friend.friendId ? "active" : "",
              unreadByFriendId[friend.friendId] ? "has-unread" : ""
            ].filter(Boolean).join(" ")}
            key={friend.friendId}
            onClick={() => {
              markFriendSeen(friend.friendId);
              onSelectFriend(friend.friendId);
            }}
          >
            <StatusDot online={isOnline(friend.friendId)} />
            {friend.friendUsername || `Korisnik ${friend.friendId}`}
            {unreadByFriendId[friend.friendId] && (
              <span className="chat-unread-badge" aria-label="Nove poruke">
                {unreadByFriendId[friend.friendId]}
              </span>
            )}
          </button>
        ))}
      </div>

      <div className="chat-log" ref={logRef}>
        {selectedFriend && messages.length === 0 && (
          <p className="muted">Nema poruka. Posalji prvu.</p>
        )}
        {selectedFriend &&
          messages.map((message) => (
            <p className={message.isOwn ? "own" : ""} key={message.id}>
              <span>{message.content}</span>
              <small>
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
