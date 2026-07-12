

import React from "react";

const dots = {

  1: [5],

  2: [1, 9],

  3: [1, 5, 9],

  4: [1, 3, 7, 9],

  5: [1, 3, 5, 7, 9],

  6: [1, 3, 4, 6, 7, 9]

};

export default function Dice({ canRoll, isRolling, onRoll, value }) {

  const currentValue = Number.isFinite(value) && value > 0 ? value : 1;

  const positions = dots[currentValue] || dots[1];

  return (

    <button

      className={`dice ${isRolling ? "rolling" : ""}`}

      disabled={!canRoll || isRolling}

      onClick={onRoll}

      title={canRoll ? "Baci kockicu" : "Cekaj svoj potez"}

      type="button"

    >

      {Array.from({ length: 9 }, (_, index) => {

        const dotNumber = index + 1;

        return (

          <span

            className={positions.includes(dotNumber) ? "visible" : ""}

            key={dotNumber}

          />

        );

      })}

    </button>

  );

}
