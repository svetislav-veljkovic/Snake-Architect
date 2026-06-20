import { useState } from "react";
import React from "react";

export default function BoardEditor({
  board,
  disabled,
  mode,
  onAdd,
  onClear,
  onModeChange,
  selectedPositions
}) {
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");

  function submit(event) {
    event.preventDefault();
    const startNum = Number(start);
    const endNum = Number(end);
    if (Number.isFinite(startNum) && Number.isFinite(endNum)) {
      onAdd(mode, startNum, endNum);
      setStart("");
      setEnd("");
    }
  }

  if (disabled) {
    return (
      <div className="panel compact">
        <p className="eyebrow">Editor table</p>
        <h2>Tabla je zakljucana</h2>
        <p className="muted">
          Kada igra pocne, zmije i merdevine vise ne mogu da se menjaju.
        </p>
      </div>
    );
  }

  const maxPosition = (board?.rows ?? 10) * (board?.columns ?? 10);

  return (
    <div className="panel compact">
      <div className="section-head">
        <div>
          <p className="eyebrow">Editor table</p>
          <h2>Dizajniraj tablu</h2>
        </div>
      </div>

      <div className="segmented small">
        <button
          type="button"
          className={mode === "ladder" ? "active" : ""}
          onClick={() => onModeChange("ladder")}
        >
          Merdevine
        </button>
        <button
          type="button"
          className={mode === "snake" ? "active" : ""}
          onClick={() => onModeChange("snake")}
        >
          Zmija
        </button>
      </div>

      <p className="muted">
        Izaberi dva polja na tabli ili unesi brojeve (1-{maxPosition}).
        {selectedPositions.length > 0 && ` Izabrano: ${selectedPositions.join(" -> ")}`}
      </p>

      <form className="stack compact-gap" onSubmit={submit}>
        <div className="inline-fields">
          <label>
            Od polja
            <input
              min="1"
              max={maxPosition}
              type="number"
              value={start}
              onChange={(event) => setStart(event.target.value)}
            />
          </label>
          <label>
            Do polja
            <input
              min="1"
              max={maxPosition}
              type="number"
              value={end}
              onChange={(event) => setEnd(event.target.value)}
            />
          </label>
        </div>
        <button type="submit" className="primary">
          Dodaj {mode === "ladder" ? "merdevine" : "zmiju"}
        </button>
      </form>

      <button type="button" className="ghost" onClick={onClear}>
        Ocisti tablu
      </button>
    </div>
  );
}
