

import React from "react";
const tokenColors = ["#2f6fed", "#d94862", "#248f67", "#8b5cf6", "#e08a1e", "#0f766e"];

function initials(name, fallback) {
  const value = name || fallback || "?";
  const parts = value.split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

export default function Cell({
  canEdit,
  ladderEnd,
  ladderStart,
  onClick,
  players,
  position,
  selected,
  snakeEnd,
  snakeStart,
  userNames
}) {
  const classNames = [
    "cell",
    snakeStart || snakeEnd ? "has-snake" : "",
    ladderStart || ladderEnd ? "has-ladder" : "",
    selected ? "selected" : "",
    canEdit ? "clickable" : ""
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classNames} onClick={() => onClick?.(position)} type="button">
      <span className="cell-number">{position}</span>

      {snakeStart && (
        <span
          className="marker snake head"
          title={`Zmija vodi do ${snakeStart.endPosition}`}
        >
          S ? {snakeStart.endPosition}
        </span>
      )}
      {snakeEnd && <span className="marker snake tail">rep</span>}

      {ladderStart && (
        <span
          className="marker ladder base"
          title={`Merdevine vode do ${ladderStart.endPosition}`}
        >
          L ? {ladderStart.endPosition}
        </span>
      )}
      {ladderEnd && <span className="marker ladder top">vrh</span>}

      <span className="players">
        {(players ?? []).map((player, index) => {
          const name = userNames?.[player.userId] || `Korisnik ${player.userId}`;
          return (
            <span
              className="player-token"
              key={player.id ?? `${player.userId}-${index}`}
              style={{ background: tokenColors[index % tokenColors.length] }}
              title={name}
            >
              {initials(name)}
            </span>
          );
        })}
      </span>
    </button>
  );
}
