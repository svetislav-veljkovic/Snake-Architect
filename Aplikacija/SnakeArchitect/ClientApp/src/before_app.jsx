import React, { useEffect, useMemo, useRef, useState } from "react";
import { createRoot } from "react-dom/client";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import "./styles.css";
const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5232";
const HUB_URL = `${API_BASE}/hubs/chat`;
const emptyLogin = { username: "", password: "" };
const emptyRegister = {
  name: "",
  lastName: "",
  username: "",
  email: "",
  password: "",
  gamesWon: 0,
  gamesLost: 0
};
function normalize(value) {
  if (Array.isArray(value)) return value.map(normalize);
  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value).map(([key, item]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        normalize(item)
      ])
    );
  }
  return value;
}
