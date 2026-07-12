import React from 'react';

const PHASES = [

  { key: 'setup', label: 'Podesavanje table' },

  { key: 'lobby', label: 'Cekaonica' },

  { key: 'playing', label: 'Igra u toku' }

];

function resolvePhaseIndex(room) {

  if (room?.isStarted) return 2;

  if (room?.boardConfirmed) return 1;

  return 0;

}

export default function PhaseStepper({ room }) {

  const activeIndex = resolvePhaseIndex(room);

  return (

    <div className="phase-stepper">

      {PHASES.map((phase, index) => (

        <React.Fragment key={phase.key}>

          <div

            className={

              'phase-step' +

              (index === activeIndex ? ' active' : '') +

              (index < activeIndex ? ' done' : '')

            }

          >

            <span className="phase-step-dot">

              {index < activeIndex ? '\u2713' : index + 1}

            </span>

            <span className="phase-step-label">{phase.label}</span>

          </div>

          {index < PHASES.length - 1 && (

            <span

              className={'phase-step-line' + (index < activeIndex ? ' filled' : '')}

            />

          )}

        </React.Fragment>

      ))}

    </div>

  );

}
