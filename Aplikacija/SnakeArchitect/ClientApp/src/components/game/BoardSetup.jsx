import { useState } from "react";

import React from "react";

export default function BoardSetup({ onCreate, onCancel }) {

  const [rows, setRows] = useState(10);

  const [columns, setColumns] = useState(10);

  const [error, setError] = useState("");

  const [busy, setBusy] = useState(false);

  async function submit(event) {

    event.preventDefault();

    const r = Number(rows);

    const c = Number(columns);

    if (!Number.isFinite(r) || !Number.isFinite(c) || r < 5 || c < 5 || r > 15 || c > 15) {

      setError("Dimenzije table moraju biti između 5 i 15.");

      return;

    }

    setError("");

    setBusy(true);

    try {

      await onCreate(r, c);

    } catch (err) {

      setError(err.message);

    } finally {

      setBusy(false);

    }

  }

  return (

    <div className="panel compact">

      <div className="section-head">

        <div>

          <p className="eyebrow">Korak 1</p>

          <h2>Podesi tablu</h2>

        </div>

      </div>

      <p className="muted">

        Pre pozivanja igrača i postavljanja zmija/merdevina, odaberi dimenzije table.

      </p>

      <form className="stack compact-gap" onSubmit={submit}>

        <div className="inline-fields">

          <label>

            Redovi

            <input max="15" min="5" type="number" value={rows} onChange={(e) => setRows(e.target.value)} />

          </label>

          <label>

            Kolone

            <input max="15" min="5" type="number" value={columns} onChange={(e) => setColumns(e.target.value)} />

          </label>

        </div>

        <button className="primary" disabled={busy} type="submit">

          {busy ? "Kreiram..." : "Kreiraj tablu"}

        </button>

      </form>

      {error && <p className="notice compact-notice">{error}</p>}

      <button type="button" className="ghost" onClick={onCancel} style={{ marginTop: 8 }}>

        Odustani od sobe

      </button>

    </div>

  );

}
