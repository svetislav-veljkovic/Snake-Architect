import Cell from "./Cell.jsx";
import React from "react";

// Build the visual numbering for the board. The board is rendered bottom-up
// (row 0 at the bottom) and each row snakes back and forth (boustrophedon),
// matching the classic snakes and ladders look.
function boardNumbers(rows, columns) {
  const numbers = [];
  for (let row = rows - 1; row >= 0; row -= 1) {
    const rowNumbers = Array.from({ length: columns }, (_, column) => row * columns + column + 1);
    if (row % 2 === 1) rowNumbers.reverse();
    numbers.push(...rowNumbers);
  }
  return numbers;
}

function displayPosition(player) {
  return Math.max(1, player?.currentPosition || 1);
}

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
  const numbers = boardNumbers(rows, columns);

  return (
    <div className="board-wrap">
      <div
        className="board-grid"
        style={{ gridTemplateColumns: `repeat(${columns}, minmax(48px, 1fr))` }}
      >
        {numbers.map((position) => {
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
              key={position}
              ladderEnd={ladderEnd}
              ladderStart={ladderStart}
              onClick={onCellClick}
              players={playersHere}
              position={position}
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
