import { useEffect, useRef } from "react";
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
