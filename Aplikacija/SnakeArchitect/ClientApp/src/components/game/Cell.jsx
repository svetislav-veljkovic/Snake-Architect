import React from "react";
const rowTints = ["tint-0", "tint-1", "tint-2", "tint-3"];
export default function Cell({
  canEdit,
  columns,
  onClick,
  position,
  rowIndex,
  selected
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
  return (
    <button className={classNames} onClick={() => onClick?.(position)} type="button">
      <span className="cell-number">{position}</span>
    </button>
  );
}
