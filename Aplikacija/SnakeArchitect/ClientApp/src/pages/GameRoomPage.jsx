import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import React from 'react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import { HUB_URL } from '../utils/api.js';
import Board from '../components/game/Board.jsx';
import BoardEditor from '../components/game/BoardEditor.jsx';
import BoardSetup from '../components/game/BoardSetup.jsx';
import Dice from '../components/game/Dice.jsx';
import Lobby from '../components/game/Lobby.jsx';
import PhaseStepper from '../components/game/PhaseStepper.jsx';
import { usePolling } from '../hooks/usePolling.js';
const POLL_INTERVAL_MS = 2000;
const BOARD_THEME_KEY = 'snakeArchitect.boardTheme';
const INTENTIONAL_LEAVE_PREFIX = 'snakeArchitect.intentionalLeave.';
const QUICK_REACTIONS = ['👍', '😂', '😱', '🔥', '😢', '🎉'];
const BOARD_THEMES = [
  { key: 'classic', label: 'Tema-1' },
  { key: 'night', label: 'Tema-2' },
  { key: 'desert', label: 'Tema-3' }
];
function moveMessage(moveType) {
  if (moveType === 'snake') return 'Stao/la si na zmiju i spustao/la se.';
  if (moveType === 'ladder') return 'Popeo/la si se uz merdevine.';
  if (moveType === 'blocked') return 'Ne moze dalje od kraja table.';
  return 'Potez je odigran.';
}
function readStoredTheme() {
  try {
    return localStorage.getItem(BOARD_THEME_KEY) || 'classic';
  } catch {
    return 'classic';
  }
}
function markIntentionalLeave(userId, roomId) {
  if (!userId || !roomId) return;
  try {
    localStorage.setItem(`${INTENTIONAL_LEAVE_PREFIX}${userId}.${roomId}`, '1');
  } catch {
  }
}
export default function GameRoomPage({
  friends,
  gameRequests,
  onAcceptGameRequest,
  onCloseRoom,
  onDeclineGameRequest,
  onRefreshDashboard,
  roomId
}) {
  const { api, token, user } = useAuth();
  const [room, setRoom] = useState(null);
  const [moves, setMoves] = useState([]);
  const [winner, setWinner] = useState(null);
  const [toasts, setToasts] = useState([]);
  const [diceValue, setDiceValue] = useState(1);
  const [isRolling, setIsRolling] = useState(false);
  const [permanentLeaveConfirmOpen, setPermanentLeaveConfirmOpen] = useState(false);
  const [editorMode, setEditorMode] = useState('ladder');
  const [selectedPositions, setSelectedPositions] = useState([]);
  const [userNames, setUserNames] = useState({});
  const [positionOverrides, setPositionOverrides] = useState({});
  const [steppingPlayerId, setSteppingPlayerId] = useState(null);
  const [reactions, setReactions] = useState([]);
  const [boardTheme, setBoardTheme] = useState(readStoredTheme);
  const boardRef = useRef(null);
  const connectionRef = useRef(null);
  const intentionallyLeftRef = useRef(false);
  useEffect(() => {
    intentionallyLeftRef.current = false;
  }, [roomId]);
  useEffect(() => {
    try {
      localStorage.setItem(BOARD_THEME_KEY, boardTheme);
    } catch {
    }
  }, [boardTheme]);
  const pushToast = useCallback((text, type = 'info') => {
    if (!text) return;
    const id = Date.now() + Math.random();
    setToasts((current) => [...current, { id, text, type }]);
    setTimeout(() => {
      setToasts((current) => current.filter((t) => t.id !== id));
    }, 3500);
  }, []);
  const toastStack = toasts.length > 0 && (
    <div className="toast-stack">
      {toasts.map((t) => (
        <div className={'toast' + (t.type === 'error' ? ' error' : '')} key={t.id}>
          {t.text}
        </div>
      ))}
    </div>
  );
  const loadRoom = useCallback(async () => {
    if (!roomId) return;
    const safe = (promise, fallback) => promise.catch(() => fallback);
    const [roomData, stateData, moveData, winnerData] = await Promise.all([
      safe(api.get('/api/GameRoom/' + roomId), null),
      safe(api.get('/api/Game/' + roomId + '/state'), null),
      safe(api.get('/api/Game/' + roomId + '/moves'), []),
      safe(api.get('/api/Game/' + roomId + '/winner'), { hasWinner: false })
    ]);
    if (!roomData) {
      pushToast('Soba nije pronadjena ili nemate pristup.', 'error');
      return;
    }
    const statePlayers = stateData?.players ?? [];
    const mergedPlayers = (roomData.players ?? []).map((player) => {
      const statePlayer = statePlayers.find((item) => item.id === player.id);
      return statePlayer ? { ...player, ...statePlayer } : player;
    });
    setRoom({
      ...roomData,
      isActive: stateData?.isActive ?? roomData.isActive,
      isPaused: stateData?.isPaused ?? roomData.isPaused,
      connectedPlayerCount: stateData?.connectedPlayerCount ?? roomData.connectedPlayerCount,
      requiredPlayerCount: stateData?.requiredPlayerCount ?? roomData.minPlayers,
      players: mergedPlayers
    });
    setMoves(moveData ?? []);
    setWinner(winnerData);
    if (roomData.players && roomData.players.length > 0) {
      const userIds = roomData.players.map((p) => p.userId).filter(Boolean);
      const map = {};
      await Promise.all(userIds.map(async (id) => {
        try {
          const u = await api.get('/api/User/' + id);
          if (u && u.username) map[id] = u.username;
        } catch (e) {  }
      }));
      setUserNames(map);
    }
  }, [api, roomId, pushToast]);
  useEffect(() => {
    loadRoom().catch((error) => pushToast(error.message, 'error'));
  }, [loadRoom, pushToast]);
  usePolling(loadRoom, POLL_INTERVAL_MS);
  useEffect(() => {
    if (!room || !user?.userId) return;
    if (intentionallyLeftRef.current) return;
    const me = (room.players ?? []).find((p) => p.userId === user.userId);
    if (me && me.isConnected === false) {
      api.post('/api/GameRoom/' + roomId + '/reconnect', {})
        .then(() => loadRoom())
        .catch(() => {});
    }
  }, [room, user?.userId, api, roomId, loadRoom]);
  const board = room?.board;
  useEffect(() => {
    boardRef.current = board;
  }, [board]);
  const clearOverride = useCallback((playerId) => {
    setPositionOverrides((current) => {
      if (!(playerId in current)) return current;
      const next = { ...current };
      delete next[playerId];
      return next;
    });
  }, []);
  const animateMove = useCallback((playerId, fromPosition, toPosition, moveType) => {
    if (!playerId) return;
    if (moveType !== 'snake' && moveType !== 'ladder') return;
    const currentBoard = boardRef.current;
    if (!currentBoard) return;
    let landedPosition = toPosition;
    if (moveType === 'snake') {
      const snake = (currentBoard.snakes ?? []).find((s) => s.endPosition === toPosition);
      if (snake) landedPosition = snake.starPosition;
    } else {
      const ladder = (currentBoard.ladders ?? []).find((l) => l.endPosition === toPosition);
      if (ladder) landedPosition = ladder.startPosition;
    }
    if (landedPosition === toPosition) return;
    const HORIZONTAL_SLIDE_MS = 650;
    const STEP_MS = 130;
    const direction = moveType === 'snake' ? -1 : 1;
    setSteppingPlayerId(playerId);
    setPositionOverrides((current) => ({ ...current, [playerId]: landedPosition }));
    let pos = landedPosition;
    const stepFurther = () => {
      pos += direction;
      setPositionOverrides((current) => ({ ...current, [playerId]: pos }));
      if (pos === toPosition) {
        setTimeout(() => {
          clearOverride(playerId);
          setSteppingPlayerId((current) => (current === playerId ? null : current));
        }, STEP_MS);
        return;
      }
      setTimeout(stepFurther, STEP_MS);
    };
    setTimeout(stepFurther, HORIZONTAL_SLIDE_MS);
  }, [clearOverride]);
  const addReaction = useCallback((userId, emoji) => {
    const id = Date.now() + Math.random();
    setReactions((current) => [...current, { id, userId, emoji }]);
    setTimeout(() => {
      setReactions((current) => current.filter((r) => r.id !== id));
    }, 1700);
  }, []);
  useEffect(() => {
    if (!token || !roomId) return undefined;
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    connectionRef.current = connection;
    connection.on('GameStarted', () => {
      pushToast('Igra je pokrenuta. Tabla je zakljucana.');
      loadRoom().catch(() => {});
    });
    connection.on('ReceiveMove', (playerId, fromPosition, toPosition, moveType) => {
      animateMove(playerId, fromPosition, toPosition, moveType);
      loadRoom().catch(() => {});
    });
    connection.on('ReceiveWinner', () => {
      loadRoom().catch(() => {});
      onRefreshDashboard();
    });
    connection.on('PlayerConnectionChanged', () => {
      loadRoom().catch(() => {});
      onRefreshDashboard();
    });
    connection.on('PlayerPermanentlyLeft', () => {
      loadRoom().catch(() => {});
      onRefreshDashboard();
    });
    connection.on('ReceiveReaction', (userId, emoji) => {
      addReaction(userId, emoji);
    });
    connection.on('RoomDeleted', (message) => {
      pushToast(message || 'Soba je otkazana.');
      onCloseRoom();
      onRefreshDashboard();
    });
    let cancelled = false;
    async function start() {
      try {
        await connection.start();
        if (cancelled) return;
        await connection.invoke('JoinGroup', 'game:' + roomId);
        if (user?.username) {
          await connection.invoke('JoinGroup', user.username);
        }
      } catch (e) {}
    }
    start();
    return () => {
      cancelled = true;
      connectionRef.current = null;
      connection.stop().catch(() => {});
    };
  }, [loadRoom, roomId, token, user?.username, onCloseRoom, onRefreshDashboard, pushToast, animateMove, addReaction]);
  function sendReaction(emoji) {
    if (!connectionRef.current || !user?.userId) return;
    connectionRef.current.invoke('SendReaction', roomId, user.userId, emoji).catch(() => {});
  }
  const players = room?.players ?? [];
  const me = players.find((p) => p.userId === user?.userId);
  const isPlayer = players.some((p) => p.userId == user?.userId);
  const isHost = players.some(
    (player) => player.userId === user?.userId && player.isHost
  );
  const canEditBoard = Boolean(isHost && room && !room.isStarted && board && !room.boardConfirmed);
  const designPhase = Boolean(!room?.isStarted && isHost && isPlayer && board && !room?.boardConfirmed);
  const lobbyUnlocked = Boolean(room?.isStarted || room?.boardConfirmed);
  const isGamePaused = Boolean(room?.isStarted && room?.isActive && room?.isPaused);
  const orderedPlayers = useMemo(
    () => [...players].sort((a, b) => (a.id ?? 0) - (b.id ?? 0)),
    [players]
  );
  const currentPlayer =
    room?.isStarted && !isGamePaused && orderedPlayers.length
      ? orderedPlayers[moves.length % orderedPlayers.length]
      : null;
  const canRoll = Boolean(
    room?.isStarted &&
      room?.isActive &&
      !isGamePaused &&
      currentPlayer?.userId === user?.userId &&
      me?.isConnected !== false &&
      !winner?.hasWinner &&
      isPlayer
  );
  if (!room) {
    return (
      <div className='empty-state'>
        {toastStack}
        <h2>Ucitam sobu...</h2>
      </div>
    );
  }
  function handleCellClick(position) {
    if (!canEditBoard) return;
    setSelectedPositions((current) => {
      if (current.includes(position)) {
        return current.filter((p) => p !== position);
      }
      if (current.length >= 2) {
        return [position];
      }
      return [...current, position];
    });
  }
  function createBoard(rows, columns) {
    return api.post('/api/GameRoom/' + roomId + '/board', { rows, columns })
      .then(() => loadRoom());
  }
  function confirmBoard() {
    return api.post('/api/GameRoom/' + roomId + '/board/confirm', {})
      .then(() => loadRoom())
      .catch((err) => pushToast(err.message, 'error'));
  }
  function addBoardElement(mode, startNum, endNum) {
    setSelectedPositions([]);
    var key = mode === 'snake' ? 'starPosition' : 'startPosition';
    var payload = {};
    payload[key] = startNum;
    payload.endPosition = endNum;
    api.post('/api/GameBoard/' + board.id + '/' + mode, payload)
      .then(() => loadRoom())
      .catch((err) => pushToast(err.message, 'error'));
  }
  function clearBoard() {
    setSelectedPositions([]);
    api.delete('/api/GameBoard/' + board.id + '/clear')
      .then(() => loadRoom())
      .catch((err) => pushToast(err.message, 'error'));
  }
  async function rollDice() {
    if (!canRoll || isRolling) return;
    setIsRolling(true);
    try {
      const result = await api.post('/api/Game/roll/' + roomId, {});
      setDiceValue(result.diceValue ?? 1);
      await loadRoom();
      onRefreshDashboard();
    } catch (error) {
      pushToast(error.message, 'error');
    } finally {
      setTimeout(() => setIsRolling(false), 600);
    }
  }
  function inviteFriend(friendId) {
    api.post('/api/GameRequest/send', {
      recipientId: friendId,
      gameRoomId: roomId
    })
      .then(() => pushToast('Poziv je poslat.'))
      .catch((err) => pushToast(err.message, 'error'));
  }
  function startGame() {
    api.post('/api/GameRoom/' + roomId + '/start', {})
      .then(() => loadRoom())
      .catch((err) => pushToast(err.message, 'error'));
  }
  async function leaveRoom() {
    try {
      await api.post('/api/GameRoom/' + roomId + '/leave', {});
    } catch (e) {}
    if (room?.isStarted) {
      intentionallyLeftRef.current = true;
      markIntentionalLeave(user?.userId, roomId);
      await loadRoom();
      pushToast('Napustio/la si partiju. Mesto je oznaceno kao diskonektovano.');
      onCloseRoom();
    } else {
      onCloseRoom();
    }
    onRefreshDashboard();
  }
  async function permanentlyLeaveRoom() {
    try {
      await api.post('/api/GameRoom/' + roomId + '/leave-permanent', {});
      intentionallyLeftRef.current = true;
      markIntentionalLeave(user?.userId, roomId);
      setPermanentLeaveConfirmOpen(false);
      onCloseRoom();
      onRefreshDashboard();
    } catch (error) {
      setPermanentLeaveConfirmOpen(false);
      pushToast(error.message, 'error');
    }
  }
  async function cancelRoom() {
    try {
      await api.delete('/api/GameRoom/' + roomId);
    } catch (e) {}
    onCloseRoom();
    onRefreshDashboard();
  }
  async function acceptRequest(requestId) {
    await onAcceptGameRequest(requestId);
    await loadRoom();
  }
  async function declineRequest(requestId) {
    await onDeclineGameRequest(requestId);
    await loadRoom();
  }
  return (
    <div className='room-layout'>
      {toastStack}
      {(winner?.hasWinner || currentPlayer || isGamePaused) && (
        <div className='topbar full-row room-statusbar'>
          <div className='turn-pill'>
            {isGamePaused
              ? 'Partija je pauzirana: cekamo igraca'
              : winner?.hasWinner
              ? 'Pobednik: ' + (winner.username || ('Korisnik ' + winner.userId))
              : 'Na potezu: ' + (userNames[currentPlayer.userId] || ('Korisnik ' + currentPlayer.userId))}
          </div>
        </div>
      )}
      {!room.isStarted && (
        <div className='full-row'>
          <PhaseStepper room={room} />
        </div>
      )}
      <div className='room-body'>
        <main className='board-area'>
          <div className="board-theme-picker">
            <div className="segmented small">
              {BOARD_THEMES.map(({ key, label }) => (
                <button
                  key={key}
                  type="button"
                  className={boardTheme === key ? 'active' : ''}
                  onClick={() => setBoardTheme(key)}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>
          <Board
            board={board}
            canEdit={canEditBoard}
            currentPlayerId={currentPlayer?.id}
            onCellClick={handleCellClick}
            players={players}
            positionOverrides={positionOverrides}
            reactions={reactions}
            selectedPositions={selectedPositions}
            steppingPlayerId={steppingPlayerId}
            theme={boardTheme}
            userNames={userNames}
          />
        </main>
        <aside className='right-panel'>
          <div className='control-column'>
            {!room.isStarted && isHost && isPlayer && !board && (
              <BoardSetup
                onCreate={createBoard}
                onCancel={cancelRoom}
              />
            )}
            {designPhase && (
              <div className='panel compact'>
                <BoardEditor
                  board={board}
                  disabled={false}
                  mode={editorMode}
                  onAdd={addBoardElement}
                  onClear={clearBoard}
                  onModeChange={setEditorMode}
                  selectedPositions={selectedPositions}
                />
                <div className="button-pair" style={{ marginTop: 12 }}>
                  <button
                    className="primary"
                    onClick={confirmBoard}
                  >
                    Kreiraj tablu
                  </button>
                  <button
                    className="ghost"
                    onClick={cancelRoom}
                  >
                    Odustani
                  </button>
                </div>
              </div>
            )}
            {lobbyUnlocked && isPlayer && (
              <div className='panel compact'>
                <div className='panel-title-row'>
                  <p className='eyebrow'>Kockica</p>
                  {room.isStarted && !isHost && (
                    <button
                      className='ghost slim'
                      onClick={() => setPermanentLeaveConfirmOpen(true)}
                      type='button'
                    >
                      Izadji iz igre
                    </button>
                  )}
                </div>
                <h2>{isGamePaused ? 'Partija je pauzirana' : canRoll ? 'Tvoj potez' : 'Cekaj potez'}</h2>
                {isGamePaused && (
                  <p className='muted'>
                    Nema dovoljno aktivnih igraca. Host moze da pozove prijatelja ili prekine partiju.
                  </p>
                )}
                <Dice
                  canRoll={canRoll}
                  isRolling={isRolling}
                  onRoll={rollDice}
                  value={diceValue}
                />
              </div>
            )}
            {room.isStarted && isPlayer && (
              <div className="panel compact quick-reactions">
                <p className="eyebrow">Brza reakcija</p>
                <div className="reaction-buttons">
                  {QUICK_REACTIONS.map((emoji) => (
                    <button key={emoji} type="button" onClick={() => sendReaction(emoji)}>
                      {emoji}
                    </button>
                  ))}
                </div>
              </div>
            )}
            {!isPlayer && (
              <div className='panel compact'>
                <p className='eyebrow'>Status</p>
                <p>Samo gledas partiju.</p>
              </div>
            )}
            {(lobbyUnlocked || (isPlayer && !isHost && !room.isStarted)) && (
              <Lobby
                canStart={isHost && players.length >= (room.minPlayers ?? 2) && !room.isStarted && Boolean(board)}
                friends={friends}
                gameRequests={gameRequests}
                isHost={isHost}
                onAcceptGameRequest={acceptRequest}
                onDeclineGameRequest={declineRequest}
                onDeleteRoom={cancelRoom}
                onInviteFriend={inviteFriend}
                onLeaveRoom={leaveRoom}
                onStartGame={startGame}
                players={players}
                room={room}
                userNames={userNames}
              />
            )}
          </div>
          <div className='activity-column'>
            <div className='panel compact'>
              <p className='eyebrow'>Potezi</p>
              <div className='timeline'>
                {moves.length === 0 && <p className='muted'>Jos nema poteza.</p>}
                {moves.slice(0, 5).map((move) => {
                  const owner = players.find((p) => p.id === move.playerId);
                  const ownerName = owner ? userNames[owner.userId] : null;
                  return (
                    <div className={'move ' + (move.moveType ?? 'normal')} key={move.id}>
                      <strong>{ownerName || ('P' + move.playerId)}</strong>
                      <span>
                        {move.fromPosition} → {move.toPosition}
                      </span>
                      <small>{moveMessage(move.moveType)}</small>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </aside>
      </div>
      {winner?.hasWinner && (
        <div className='winner-overlay'>
          <div className='winner-modal'>
            <p className='eyebrow'>Kraj igre</p>
            <h2>{(winner.username || ('Korisnik ' + winner.userId)) + ' je pobedio!'}</h2>
            <button
              className='primary'
              onClick={() => {
                onRefreshDashboard();
                onCloseRoom();
              }}
            >
              Nazad na pocetnu
            </button>
          </div>
        </div>
      )}
      {permanentLeaveConfirmOpen && (
        <div className="confirm-overlay" role="dialog" aria-modal="true">
          <div className="confirm-modal">
            <p className="eyebrow">Potvrda</p>
            <h2>Izadji iz igre?</h2>
            <p>
              Da li stvarno zelis da trajno napustis ovu igru? Host ce moci da primi
              drugog igraca na tvoje mesto.
            </p>
            <span className="button-pair">
              <button className="ghost" onClick={() => setPermanentLeaveConfirmOpen(false)}>
                Odustani
              </button>
              <button className="danger" onClick={permanentlyLeaveRoom}>
                Izadji iz igre
              </button>
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
