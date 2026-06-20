import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import React from 'react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import { HUB_URL } from '../utils/api.js';
import Board from '../components/game/Board.jsx';
import BoardEditor from '../components/game/BoardEditor.jsx';
import Dice from '../components/game/Dice.jsx';
import Lobby from '../components/game/Lobby.jsx';
import { usePolling } from '../hooks/usePolling.js';

const POLL_INTERVAL_MS = 2000;

function maxPosition(board) {
  return (board?.rows ?? 10) * (board?.columns ?? 10);
}

function moveMessage(moveType) {
  if (moveType === 'snake') return 'Stao/la si na zmiju i spustao/la se.';
  if (moveType === 'ladder') return 'Popeo/la si se uz merdevine.';
  if (moveType === 'blocked') return 'Ne moze dalje od kraja table.';
  return 'Potez je odigran.';
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
  const [notice, setNotice] = useState('');
  const [diceValue, setDiceValue] = useState(1);
  const [isRolling, setIsRolling] = useState(false);
  const [editorMode, setEditorMode] = useState('ladder');
  const [selectedPositions, setSelectedPositions] = useState([]);
  const [userNames, setUserNames] = useState({});

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
      setNotice('Soba nije pronadjena ili nemate pristup.');
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
      players: mergedPlayers
    });
    setMoves(moveData ?? []);
    setWinner(winnerData);

    // Fetch usernames for all players in the room
    if (roomData.players && roomData.players.length > 0) {
      const userIds = roomData.players.map((p) => p.userId).filter(Boolean);
      const map = {};
      await Promise.all(userIds.map(async (id) => {
        try {
          const u = await api.get('/api/User/' + id);
          if (u && u.username) map[id] = u.username;
        } catch (e) { /* ignore */ }
      }));
      setUserNames(map);
    }
  }, [api, roomId]);

  useEffect(() => {
    loadRoom().catch((error) => setNotice(error.message));
  }, [loadRoom]);

  usePolling(loadRoom, POLL_INTERVAL_MS);

  useEffect(() => {
    if (!token || !roomId) return undefined;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('GameStarted', () => {
      setNotice('Igra je pokrenuta. Tabla je zakljucana.');
      loadRoom().catch(() => {});
    });
    connection.on('ReceiveMove', (_playerId, _from, _to, moveType) => {
      setNotice(moveMessage(moveType));
      loadRoom().catch(() => {});
    });
    connection.on('ReceiveWinner', () => {
      loadRoom().catch(() => {});
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
      connection.stop().catch(() => {});
    };
  }, [loadRoom, roomId, token, user?.username]);

  const players = room?.players ?? [];
  const board = room?.board;
  const isHost = players.some(
    (player) => player.userId === user?.userId && player.isHost
  );
  const canEditBoard = Boolean(isHost && room && !room.isStarted);
  const orderedPlayers = useMemo(
    () => [...players].sort((a, b) => (a.id ?? 0) - (b.id ?? 0)),
    [players]
  );
  const currentPlayer =
    room?.isStarted && orderedPlayers.length
      ? orderedPlayers[moves.length % orderedPlayers.length]
      : null;
  const canRoll = Boolean(
    room?.isStarted &&
      room?.isActive &&
      currentPlayer?.userId === user?.userId &&
      !winner?.hasWinner
  );

  if (!room) {
    return (
      <div className='empty-state'>
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

  function addBoardElement(mode, startNum, endNum) {
    setSelectedPositions([]);
    setNotice('');
    // SnakeDTO.StarPosition vs LadderDTO.StartPosition
    var key = mode === 'snake' ? 'starPosition' : 'startPosition';
    var payload = {};
    payload[key] = startNum;
    payload.endPosition = endNum;
    api.post('/api/GameBoard/' + board.id + '/' + mode, payload)
      .then(() => loadRoom())
      .catch((err) => setNotice(err.message));
  }

  function clearBoard() {
    setSelectedPositions([]);
    api.delete('/api/GameBoard/' + board.id + '/clear')
      .then(() => loadRoom())
      .catch((err) => setNotice(err.message));
  }

  async function rollDice() {
    if (!canRoll) return;
    setIsRolling(true);
    setNotice('');
    try {
      const result = await api.post('/api/Game/roll/' + roomId, {});
      setDiceValue(result.diceValue ?? 1);
      setNotice(result.message || moveMessage(result.moveType));
      await loadRoom();
      onRefreshDashboard();
    } catch (error) {
      setNotice(error.message);
    } finally {
      setTimeout(() => setIsRolling(false), 500);
    }
  }

  function inviteFriend(friendId) {
    api.post('/api/GameRequest/send', {
      recipientId: friendId,
      gameRoomId: roomId
    })
      .then(() => setNotice('Poziv je poslat.'))
      .catch((err) => setNotice(err.message));
  }

  function startGame() {
    api.post('/api/GameRoom/' + roomId + '/start', {})
      .then(() => loadRoom())
      .catch((err) => setNotice(err.message));
  }

  async function leaveRoom() {
    try {
      await api.post('/api/GameRoom/' + roomId + '/leave', {});
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
      <div className='topbar full-row'>
        <div>
          <p className='eyebrow'>
            {room.isStarted ? 'Gameplay mod' : 'Dizajn table'}
          </p>
          <h1>{room.name}</h1>
        </div>
        <div className='turn-pill'>
          {winner?.hasWinner
            ? 'Pobednik: ' + (winner.username || ('Korisnik ' + winner.userId))
            : currentPlayer
              ? 'Na potezu: ' + (userNames[currentPlayer.userId] || ('Korisnik ' + currentPlayer.userId))
              : 'Igra jos nije pocela'}
        </div>
      </div>

      {notice && <p className='notice full-row'>{notice}</p>}

      <div className='room-body'>
        <main className='board-area'>
          <Board
            board={board}
            canEdit={canEditBoard}
            onCellClick={handleCellClick}
            players={players}
            selectedPositions={selectedPositions}
            userNames={userNames}
          />
        </main>

        <aside className='right-panel'>
          <div className='control-column'>
            {!room.isStarted && isHost ? (
              <BoardEditor
                board={board}
                disabled={!canEditBoard}
                mode={editorMode}
                onAdd={addBoardElement}
                onClear={clearBoard}
                onModeChange={setEditorMode}
                selectedPositions={selectedPositions}
              />
            ) : (
              <div className='panel compact'>
                <p className='eyebrow'>Kockica</p>
                <h2>{canRoll ? 'Tvoj potez' : 'Cekaj potez'}</h2>
                <Dice
                  canRoll={canRoll}
                  isRolling={isRolling}
                  onRoll={rollDice}
                  value={diceValue}
                />
                <p className='muted'>
                  {room.isStarted
                    ? canRoll
                      ? 'Baci kockicu i pomeri se.'
                      : 'Dugme je aktivno samo kada je tvoj red.'
                    : 'Host prvo zakljucava tablu pokretanjem igre.'}
                </p>
              </div>
            )}

            <Lobby
              canStart={isHost && players.length >= 2 && !room.isStarted}
              friends={friends}
              gameRequests={gameRequests}
              isHost={isHost}
              onAcceptGameRequest={acceptRequest}
              onDeclineGameRequest={declineRequest}
              onInviteFriend={inviteFriend}
              onLeaveRoom={leaveRoom}
              onStartGame={startGame}
              players={players}
              room={room}
              userNames={userNames}
            />
          </div>

          <div className='activity-column'>
            <div className='panel compact'>
              <p className='eyebrow'>Potezi</p>
              <h2>Timeline</h2>
              <div className='timeline'>
                {moves.length === 0 && <p className='muted'>Jos nema poteza.</p>}
                {moves.slice(0, 8).map((move) => {
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
            <button className='primary' onClick={onCloseRoom}>
              Nazad na pocetnu
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
