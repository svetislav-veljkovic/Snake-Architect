import Cell from "./Cell.jsx";
import React, { useMemo } from "react";

// Build the visual numbering for the board. The board is rendered bottom-up
// (row 0 at the bottom) and each row snakes back and forth (boustrophedon),
// matching the classic snakes and ladders look.
function boardNumbers(rows, columns) {
  const cells = [];
  for (let row = rows - 1; row >= 0; row -= 1) {
    const rowNumbers = Array.from({ length: columns }, (_, column) => row * columns + column + 1);
    if (row % 2 === 1) rowNumbers.reverse();
    rowNumbers.forEach((position, column) =>
      cells.push({ position, row, column })
    );
  }
  return cells;
}

function displayPosition(player) {
  return Math.max(1, player?.currentPosition || 1);
}

function positionToXY(position, rows, columns) {
  if (!position || position < 1) return null;
  const row = Math.floor((position - 1) / columns);
  const localColumn = (position - 1) % columns;
  const visualRow = rows - 1 - row;
  const visualColumn = row % 2 === 0 ? localColumn : columns - 1 - localColumn;
  return { row: visualRow, column: visualColumn };
}

function buildOverlayPaths(rows, columns, snakes, ladders, cellSize, gap) {
  const step = cellSize + gap;
  const snakesData = snakes
    .map((snake) => {
      const start = positionToXY(snake.starPosition, rows, columns);
      const end = positionToXY(snake.endPosition, rows, columns);
      if (!start || !end) return null;
      const sx = start.column * step + cellSize / 2;
      const sy = start.row * step + cellSize / 2;
      const ex = end.column * step + cellSize / 2;
      const ey = end.row * step + cellSize / 2;
      const dx = ex - sx;
      const dy = ey - sy;
      const length = Math.sqrt(dx * dx + dy * dy) || 1;
      const ux = dx / length;
      const uy = dy / length;
      const nx = -uy;
      const ny = ux;
      const arcOffset = length * 0.35;
      const c1x = sx + ux * (length * 0.33) + nx * arcOffset;
      const c1y = sy + uy * (length * 0.33) + ny * arcOffset;
      const c2x = sx + ux * (length * 0.66) - nx * arcOffset;
      const c2y = sy + uy * (length * 0.66) - ny * arcOffset;
      let d = 'M ' + sx + ' ' + sy;
      d += ' C ' + c1x + ' ' + c1y + ' ' + c2x + ' ' + c2y + ' ' + ex + ' ' + ey;
      return {
        d,
        headX: sx,
        headY: sy,
        tailX: ex,
        tailY: ey,
        tailDirX: ux,
        tailDirY: uy
      };
    })
    .filter(Boolean);

  const ladderPaths = ladders
    .map((ladder) => {
      const start = positionToXY(ladder.startPosition, rows, columns);
      const end = positionToXY(ladder.endPosition, rows, columns);
      if (!start || !end) return null;
      const sx = start.column * step + cellSize / 2;
      const sy = start.row * step + cellSize / 2;
      const ex = end.column * step + cellSize / 2;
      const ey = end.row * step + cellSize / 2;
      const dx = ex - sx;
      const dy = ey - sy;
      const length = Math.sqrt(dx * dx + dy * dy) || 1;
      const ux = dx / length;
      const uy = dy / length;
      const nx = -uy;
      const ny = ux;
      const railOffset = Math.min(cellSize * 0.32, 14);
      const r1x = sx + nx * railOffset;
      const r1y = sy + ny * railOffset;
      const r2x = sx - nx * railOffset;
      const r2y = sy - ny * railOffset;
      const r1ex = ex + nx * railOffset;
      const r1ey = ey + ny * railOffset;
      const r2ex = ex - nx * railOffset;
      const r2ey = ey - ny * railOffset;
      let d = 'M ' + r1x + ' ' + r1y + ' L ' + r1ex + ' ' + r1ey;
      d += ' M ' + r2x + ' ' + r2y + ' L ' + r2ex + ' ' + r2ey;
      const rungSpacing = Math.max(14, cellSize * 0.45);
      const rungCount = Math.max(2, Math.floor(length / rungSpacing));
      for (let i = 1; i < rungCount; i++) {
        const t = i / rungCount;
        const mx = sx + ux * (length * t);
        const my = sy + uy * (length * t);
        const rx1 = mx + nx * railOffset;
        const ry1 = my + ny * railOffset;
        const rx2 = mx - nx * railOffset;
        const ry2 = my - ny * railOffset;
        d += ' M ' + rx1 + ' ' + ry1 + ' L ' + rx2 + ' ' + ry2;
      }
      return d;
    })
    .filter(Boolean);

  return { snakes: snakesData, ladderPaths };
}

const tokenColors = [
  "#2f6fed",
  "#d94862",
  "#248f67",
  "#8b5cf6",
  "#e08a1e",
  "#0f766e",
  "#ec4899",
  "#14b8a6",
  "#f59e0b",
  "#6366f1"
];

function colorForUser(userId) {
  const key = String(userId);
  let hash = 0;
  for (let i = 0; i < key.length; i++) {
    hash = (hash * 31 + key.charCodeAt(i)) | 0;
  }
  return tokenColors[Math.abs(hash) % tokenColors.length];
}

function initials(name, fallback) {
  const value = name || fallback || "?";
  const parts = value.split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

function PawnIcon() {
  return (
    <svg className="pawn-icon" viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="7.2" r="3.6" />
      <path d="M5.6 19c1.2-3.4 3.7-5 6.4-5s5.2 1.6 6.4 5z" />
    </svg>
  );
}

// Grupise igrace po trenutnoj poziciji da bi mogli da se rasporede jedan
// pored drugog unutar iste celije umesto da se preklapaju.
function groupPlayersByPosition(players) {
  const map = new Map();
  (players ?? []).forEach((player) => {
    const pos = displayPosition(player);
    if (!map.has(pos)) map.set(pos, []);
    map.get(pos).push(player);
  });
  return map;
}

const CELL_SIZE = 76;
const CELL_GAP = 5;
const TOKEN_SIZE = 30;
const TOKEN_SPACING = 20;

export default function Board({
  board,
  canEdit,
  onCellClick,
  players,
  selectedPositions,
  userNames
}) {
  if (!board) {
    return (
      <div className="board-empty">
        <p>Tabla nije ucitana.</p>
      </div>
    );
  }

  const rows = board.rows ?? board.row ?? 10;
  const columns = board.columns ?? 10;
  const snakes = board.snakes ?? [];
  const ladders = board.ladders ?? [];
  const cells = boardNumbers(rows, columns);
  const step = CELL_SIZE + CELL_GAP;

  const totalWidth = columns * CELL_SIZE + (columns - 1) * CELL_GAP;
  const totalHeight = rows * CELL_SIZE + (rows - 1) * CELL_GAP;
  const gridStyle = {
    position: "relative",
    display: "grid",
    gridTemplateColumns: "repeat(" + columns + ", " + CELL_SIZE + "px)",
    gap: CELL_GAP + "px",
    width: totalWidth + "px"
  };
  const overlay = useMemo(
    () => buildOverlayPaths(rows, columns, snakes, ladders, CELL_SIZE, CELL_GAP),
    [rows, columns, snakes, ladders]
  );

  const playerGroups = useMemo(() => groupPlayersByPosition(players), [players]);

  // FIX: igraci se sad crtaju u zasebnom apsolutno pozicioniranom sloju
  // preko table (isto kao zmije/merdevine), umesto unutar Cell-a. Svaki
  // token ima stabilan React key (player.id) i CSS transition na
  // left/top, pa kad se currentPosition promeni (posle bacanja kockice),
  // token animirano "klizi" od stare do nove celije umesto da se trenutno
  // pojavi na novom mestu.
  const playerTokens = useMemo(() => {
    const tokens = [];
    playerGroups.forEach((group, position) => {
      const xy = positionToXY(position, rows, columns);
      if (!xy) return;
      const baseX = xy.column * step + CELL_SIZE / 2;
      const baseY = xy.row * step + CELL_SIZE / 2;
      const count = group.length;
      group.forEach((player, index) => {
        const offset = (index - (count - 1) / 2) * TOKEN_SPACING;
        tokens.push({ player, x: baseX + offset, y: baseY });
      });
    });
    return tokens;
  }, [playerGroups, rows, columns, step]);

  return (
    <div
      className="board-wrap"
      style={{
        position: "relative",
        overflow: "auto",
        width: "max-content"
      }}
    >
      <svg
        width={totalWidth}
        height={totalHeight}
        viewBox={"0 0 " + totalWidth + " " + totalHeight}
        xmlns="http://www.w3.org/2000/svg"
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          pointerEvents: "none",
          zIndex: 5
        }}
      >
        {overlay.snakes.map((item, i) => (
          <g key={"snake-" + i}>
            <path d={item.d} fill="none" stroke="#5a0f1f" strokeWidth="14" strokeLinecap="round" opacity="0.85" />
            <path d={item.d} fill="none" stroke="#d94560" strokeWidth="11" strokeLinecap="round" opacity="1" />
            <path
              d={item.d}
              fill="none"
              stroke="#ffb3bf"
              strokeWidth="3"
              strokeLinecap="round"
              opacity="0.7"
              strokeDasharray="6 10"
            />
            <ellipse
              cx={item.headX}
              cy={item.headY}
              rx="16"
              ry="11"
              fill="#d94560"
              stroke="#5a0f1f"
              strokeWidth="2.5"
              transform={"rotate(" + (Math.atan2(item.tailDirY, item.tailDirX) * 180 / Math.PI) + " " + item.headX + " " + item.headY + ")"}
            />
            <ellipse
              cx={item.headX - 5}
              cy={item.headY - 3}
              rx="2.4"
              ry="2.2"
              fill="#fff5f6"
              transform={"rotate(" + (Math.atan2(item.tailDirY, item.tailDirX) * 180 / Math.PI) + " " + (item.headX - 5) + " " + (item.headY - 3) + ")"}
            />
            <ellipse
              cx={item.headX + 5}
              cy={item.headY - 3}
              rx="2.4"
              ry="2.2"
              fill="#fff5f6"
              transform={"rotate(" + (Math.atan2(item.tailDirY, item.tailDirX) * 180 / Math.PI) + " " + (item.headX + 5) + " " + (item.headY - 3) + ")"}
            />
            <ellipse
              cx={item.headX - 5}
              cy={item.headY - 3}
              rx="1"
              ry="0.9"
              fill="#1a0509"
              transform={"rotate(" + (Math.atan2(item.tailDirY, item.tailDirX) * 180 / Math.PI) + " " + (item.headX - 5) + " " + (item.headY - 3) + ")"}
            />
            <ellipse
              cx={item.headX + 5}
              cy={item.headY - 3}
              rx="1"
              ry="0.9"
              fill="#1a0509"
              transform={"rotate(" + (Math.atan2(item.tailDirY, item.tailDirX) * 180 / Math.PI) + " " + (item.headX + 5) + " " + (item.headY - 3) + ")"}
            />
            {(() => {
              const tipX = item.headX + item.tailDirX * 18;
              const tipY = item.headY + item.tailDirY * 18;
              const baseX = item.headX + item.tailDirX * 13;
              const baseY = item.headY + item.tailDirY * 13;
              const forkLen = 6;
              const perpX = -item.tailDirY;
              const perpY = item.tailDirX;
              const f1x = tipX + item.tailDirX * forkLen + perpX * 3;
              const f1y = tipY + item.tailDirY * forkLen + perpY * 3;
              const f2x = tipX + item.tailDirX * forkLen - perpX * 3;
              const f2y = tipY + item.tailDirY * forkLen - perpY * 3;
              return (
                <g>
                  <line x1={baseX} y1={baseY} x2={tipX} y2={tipY} stroke="#c41e3a" strokeWidth="2.2" strokeLinecap="round" />
                  <line x1={tipX} y1={tipY} x2={f1x} y2={f1y} stroke="#c41e3a" strokeWidth="2" strokeLinecap="round" />
                  <line x1={tipX} y1={tipY} x2={f2x} y2={f2y} stroke="#c41e3a" strokeWidth="2" strokeLinecap="round" />
                </g>
              );
            })()}
          </g>
        ))}
        {overlay.ladderPaths.map((d, i) => (
          <g key={"ladder-" + i}>
            <path d={d} fill="none" stroke="#3a1a06" strokeWidth="13" strokeLinecap="round" opacity="0.5" />
            <path d={d} fill="none" stroke="#a0522d" strokeWidth="10" strokeLinecap="round" opacity="0.95" />
          </g>
        ))}
      </svg>

      <div className="board-grid" style={{ ...gridStyle, zIndex: 1 }}>
        {cells.map(({ position, row, column }) => (
          <Cell
            canEdit={canEdit}
            columns={columns}
            key={position}
            onClick={onCellClick}
            position={position}
            rowIndex={row}
            selected={(selectedPositions ?? []).includes(position)}
          />
        ))}
      </div>

      <div
        className="player-layer"
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          width: totalWidth,
          height: totalHeight,
          zIndex: 6,
          pointerEvents: "none"
        }}
      >
        {playerTokens.map(({ player, x, y }) => {
          const name = userNames?.[player.userId] || `Korisnik ${player.userId}`;
          return (
            <div
              className="player-token-anim"
              key={player.id ?? player.userId}
              style={{
                left: x,
                top: y,
                width: TOKEN_SIZE,
                height: TOKEN_SIZE,
                marginLeft: -(TOKEN_SIZE / 2),
                marginTop: -(TOKEN_SIZE / 2),
                background: colorForUser(player.userId)
              }}
              title={name}
            >
              <PawnIcon />
              <small>{initials(name)}</small>
            </div>
          );
        })}
      </div>
    </div>
  );
}