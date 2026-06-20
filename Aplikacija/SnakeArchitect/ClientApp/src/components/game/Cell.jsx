import React from "react";

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

const rowTints = ["tint-0", "tint-1", "tint-2", "tint-3"];

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

export default function Cell({
  canEdit,
  columns,
  onClick,
  players,
  position,
  rowIndex,
  selected,
  userNames
}) {
  const cols = columns || 10;
  const rowFromPos =
    typeof rowIndex === "number"
      ? rowIndex
      : Math.floor((position - 1) / cols);
  const tintClass = rowTints[rowFromPos % rowTints.length];

  const classNames = [
    "cell",
    tintClass,
    selected ? "selected" : "",
    canEdit ? "clickable" : ""
  ]
    .filter(Boolean)
    .join(" ");

  const playersHere = players ?? [];

  return (
    <button className={classNames} onClick={() => onClick?.(position)} type="button">
      <span className="cell-number">{position}</span>

      <span className="players">
        {playersHere.map((player, index) => {
          const name = userNames?.[player.userId] || `Korisnik ${player.userId}`;
          return (
            <span
              className="player-token"
              key={player.id ?? `${player.userId}-${index}`}
              style={{ background: colorForUser(player.userId) }}
              title={name}
            >
              <PawnIcon />
              <small>{initials(name)}</small>
            </span>
          );
        })}
      </span>
    </button>
  );
}
