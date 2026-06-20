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
      // Smooth S-curve using cubic Bezier - single elegant curve
      // Control points pulled perpendicular to the line for a graceful arc
      const arcOffset = length * 0.35;
      // First control point: 1/3 of the way, offset perpendicular
      const c1x = sx + ux * (length * 0.33) + nx * arcOffset;
      const c1y = sy + uy * (length * 0.33) + ny * arcOffset;
      // Second control point: 2/3 of the way, offset perpendicular (opposite direction for S-curve)
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

const CELL_SIZE = 76;
const CELL_GAP = 5;

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
            {/* Dark outline for depth */}
            <path
              d={item.d}
              fill="none"
              stroke="#5a0f1f"
              strokeWidth="14"
              strokeLinecap="round"
              opacity="0.85"
            />
            {/* Body */}
            <path
              d={item.d}
              fill="none"
              stroke="#d94560"
              strokeWidth="11"
              strokeLinecap="round"
              opacity="1"
            />
            {/* Highlight along the spine */}
            <path
              d={item.d}
              fill="none"
              stroke="#ffb3bf"
              strokeWidth="3"
              strokeLinecap="round"
              opacity="0.7"
              strokeDasharray="6 10"
            />
            {/* Head as ellipse, stretched along line direction (tangent) */}
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
            {/* Forked tongue - sticks out from the leading edge of the head */}
            {(() => {
              const angle = Math.atan2(item.tailDirY, item.tailDirX);
              // Tip of head in direction of motion
              const tipX = item.headX + item.tailDirX * 18;
              const tipY = item.headY + item.tailDirY * 18;
              // Tongue base just past the head
              const baseX = item.headX + item.tailDirX * 13;
              const baseY = item.headY + item.tailDirY * 13;
              // Fork split points (perpendicular to motion)
              const forkLen = 6;
              const perpX = -item.tailDirY;
              const perpY = item.tailDirX;
              const f1x = tipX + item.tailDirX * forkLen + perpX * 3;
              const f1y = tipY + item.tailDirY * forkLen + perpY * 3;
              const f2x = tipX + item.tailDirX * forkLen - perpX * 3;
              const f2y = tipY + item.tailDirY * forkLen - perpY * 3;
              return (
                <g>
                  {/* Stem */}
                  <line
                    x1={baseX}
                    y1={baseY}
                    x2={tipX}
                    y2={tipY}
                    stroke="#c41e3a"
                    strokeWidth="2.2"
                    strokeLinecap="round"
                  />
                  {/* Left fork */}
                  <line
                    x1={tipX}
                    y1={tipY}
                    x2={f1x}
                    y2={f1y}
                    stroke="#c41e3a"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                  {/* Right fork */}
                  <line
                    x1={tipX}
                    y1={tipY}
                    x2={f2x}
                    y2={f2y}
                    stroke="#c41e3a"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                </g>
              );
            })()}
          </g>
        ))}
        {overlay.ladderPaths.map((d, i) => (
          <g key={"ladder-" + i}>
            <path
              d={d}
              fill="none"
              stroke="#3a1a06"
              strokeWidth="13"
              strokeLinecap="round"
              opacity="0.5"
            />
            <path
              d={d}
              fill="none"
              stroke="#a0522d"
              strokeWidth="10"
              strokeLinecap="round"
              opacity="0.95"
            />
          </g>
        ))}
      </svg>

      <div className="board-grid" style={{ ...gridStyle, zIndex: 1 }}>
        {cells.map(({ position, row, column }) => {
          const snakeStart = snakes.find((snake) => snake.starPosition === position);
          const snakeEnd = snakes.find((snake) => snake.endPosition === position);
          const ladderStart = ladders.find((ladder) => ladder.startPosition === position);
          const ladderEnd = ladders.find((ladder) => ladder.endPosition === position);
          const playersHere = (players ?? []).filter(
            (player) => displayPosition(player) === position
          );

          return (
            <Cell
              canEdit={canEdit}
              columns={columns}
              key={position}
              ladderEnd={ladderEnd}
              ladderStart={ladderStart}
              onClick={onCellClick}
              players={playersHere}
              position={position}
              rowIndex={row}
              selected={(selectedPositions ?? []).includes(position)}
              snakeEnd={snakeEnd}
              snakeStart={snakeStart}
              userNames={userNames}
            />
          );
        })}
      </div>
    </div>
  );
}
