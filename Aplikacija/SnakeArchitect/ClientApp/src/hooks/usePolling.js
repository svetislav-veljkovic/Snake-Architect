import { useEffect, useRef } from "react";

// Refresh at a fixed interval while a component is mounted.
// Errors are swallowed here so a single failing tick does not stop the loop.
export function usePolling(callback, delayMs) {
  const saved = useRef(callback);

  useEffect(() => {
    saved.current = callback;
  }, [callback]);

  useEffect(() => {
    if (!delayMs) return undefined;
    let cancelled = false;
    const tick = () => {
      if (cancelled) return;
      Promise.resolve(saved.current?.()).catch(() => {});
    };
    tick();
    const timer = setInterval(tick, delayMs);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [delayMs]);
}
