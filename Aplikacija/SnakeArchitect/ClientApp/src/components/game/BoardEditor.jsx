import { useEffect, useState } from "react";

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

  const [error, setError] = useState("");

  const maxPosition = (board?.rows ?? 10) * (board?.columns ?? 10);

  function validate(startNum, endNum) {

    if (!Number.isFinite(startNum) || !Number.isFinite(endNum)) {

      return "Unesi oba broja polja.";

    }

    if (startNum < 1 || endNum < 1 || startNum > maxPosition || endNum > maxPosition) {

      return "Pozicija van granica table (1-" + maxPosition + ").";

    }

    if (startNum === endNum) {

      return "Polja moraju biti razlicita.";

    }

    if (mode === "snake" && startNum <= endNum) {

      return "Zmija ide NADOLJE: glava mora biti na VECOJ poziciji od repa.";

    }

    if (mode === "ladder" && startNum >= endNum) {

      return "Merdevine idu NAGORE: dno mora biti na MANJOJ poziciji od vrha.";

    }

    return "";

  }

  useEffect(() => {

    if (selectedPositions.length === 2) {

      setStart(String(selectedPositions[0]));

      setEnd(String(selectedPositions[1]));

    }

  }, [selectedPositions]);

  function submit(event) {

    event.preventDefault();

    const startNum = Number(start);

    const endNum = Number(end);

    const msg = validate(startNum, endNum);

    if (msg) {

      setError(msg);

      return;

    }

    setError("");

    onAdd(mode, startNum, endNum);

    setStart("");

    setEnd("");

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

  return (

    <div className="panel compact">

      <div className="section-head">

        <div>

          <h2>Dizajniraj tablu</h2>

        </div>

      </div>

      <div className="segmented small">

        <button

          type="button"

          className={mode === "ladder" ? "active" : ""}

          onClick={() => { onModeChange("ladder"); setError(""); }}

        >

          Merdevine

        </button>

        <button

          type="button"

          className={mode === "snake" ? "active" : ""}

          onClick={() => { onModeChange("snake"); setError(""); }}

        >

          Zmija

        </button>

      </div>

      <p className="muted">

        {mode === "snake"

          ? "Zmija: glava na vecoj poziciji, rep na manjoj (ide nadole)."

          : "Merdevine: dno na manjoj poziciji, vrh na vecoj (ide nagore)."}

        {" Klikni dva polja na tabli da automatski popuni."}

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

      {error && <p className="notice compact-notice">{error}</p>}

      <button type="button" className="ghost" onClick={onClear}>

        Ocisti tablu

      </button>

    </div>

  );

}
