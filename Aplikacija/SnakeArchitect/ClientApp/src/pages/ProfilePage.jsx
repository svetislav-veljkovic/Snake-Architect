import { useState } from "react";
import React from "react";
import { useAuth } from "../context/AuthContext.jsx";

function initials(name, fallback) {
  const value = name || fallback || "?";
  const parts = value.split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("Ne mogu da procitam sliku."));
    reader.readAsDataURL(file);
  });
}

function userToForm(user) {
  return {
    name: user?.name || "",
    lastName: user?.lastName || "",
    username: user?.username || "",
    email: user?.email || ""
  };
}

export default function ProfilePage({ onClose }) {
  const { api, user, refreshProfile, replaceToken } = useAuth();

  const [form, setForm] = useState(() => userToForm(user));
  const [picture, setPicture] = useState(user?.profilePicture || null);
  const [pictureChanged, setPictureChanged] = useState(false);
  const [savingPicture, setSavingPicture] = useState(false);
  const [editingProfile, setEditingProfile] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [profileNotice, setProfileNotice] = useState("");

  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: ""
  });
  const [editingPassword, setEditingPassword] = useState(false);
  const [savingPassword, setSavingPassword] = useState(false);
  const [passwordNotice, setPasswordNotice] = useState("");

  function resetProfileEdit() {
    setForm(userToForm(user));
    setPicture(user?.profilePicture || null);
    setPictureChanged(false);
    setEditingProfile(false);
  }

  function resetPasswordEdit() {
    setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
    setEditingPassword(false);
  }

  async function handlePictureChange(event) {
    const file = event.target.files?.[0];
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) {
      setProfileNotice("Slika ne sme biti veca od 2MB.");
      return;
    }
    try {
      const dataUrl = await readFileAsDataUrl(file);
      setPicture(dataUrl);
      setPictureChanged(true);
      setProfileNotice("");
    } catch (err) {
      setProfileNotice(err.message);
    }
  }

  async function savePicture() {
    setProfileNotice("");
    setSavingPicture(true);
    try {
      const result = await api.put(`/api/User/${user.userId}`, {
        name: form.name,
        lastName: form.lastName,
        username: form.username,
        email: form.email,
        password: "",
        profilePicture: picture,
        gamesWon: 0,
        gamesLost: 0
      });
      if (result?.token) replaceToken(result.token);
      const refreshed = await refreshProfile();
      setPicture(refreshed?.profilePicture || picture);
      setPictureChanged(false);
      setProfileNotice("Profilna slika je sacuvana.");
    } catch (err) {
      setProfileNotice(err.message);
    } finally {
      setSavingPicture(false);
    }
  }

  async function saveProfile(event) {
    event.preventDefault();
    setProfileNotice("");
    setSavingProfile(true);
    try {
      const result = await api.put(`/api/User/${user.userId}`, {
        name: form.name,
        lastName: form.lastName,
        username: form.username,
        email: form.email,
        password: "",
        profilePicture: picture,
        gamesWon: 0,
        gamesLost: 0
      });
      if (result?.token) replaceToken(result.token);
      const refreshed = await refreshProfile();
      setPicture(refreshed?.profilePicture || picture);
      setForm(userToForm(refreshed || { ...form, profilePicture: picture }));
      setPictureChanged(false);
      setProfileNotice("Profil je azuriran.");
      setEditingProfile(false);
    } catch (err) {
      setProfileNotice(err.message);
    } finally {
      setSavingProfile(false);
    }
  }

  async function savePassword(event) {
    event.preventDefault();
    setPasswordNotice("");

    if (passwordForm.newPassword.length < 6) {
      setPasswordNotice("Nova lozinka mora imati najmanje 6 znakova.");
      return;
    }
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordNotice("Nova lozinka i potvrda se ne poklapaju.");
      return;
    }

    setSavingPassword(true);
    try {
      await api.put(`/api/User/${user.userId}/password`, {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword
      });
      resetPasswordEdit();
      setPasswordNotice("Lozinka je uspesno promenjena.");
    } catch (err) {
      setPasswordNotice(err.message);
    } finally {
      setSavingPassword(false);
    }
  }

  return (
    <div className="stack">
      <div className="topbar">
        <div>
          <h1>Moj profil</h1>
        </div>
      </div>

      <div className="profile-layout">
        <section className="panel profile-picture-panel">
          <div className="section-head">
            <div>
              <p className="eyebrow">Profilna slika</p>
            </div>
          </div>
          <div className="profile-avatar-row">
            <span className="avatar avatar-lg">
              {picture ? (
                <img src={picture} alt="Profilna slika" />
              ) : (
                <span>{initials(form.name, form.username)}</span>
              )}
            </span>
            <label className="ghost slim profile-upload">
              Promeni sliku
              <input
                accept="image/*"
                onChange={handlePictureChange}
                style={{ display: "none" }}
                type="file"
              />
            </label>
            {pictureChanged && (
              <button
                className="primary slim"
                disabled={savingPicture}
                onClick={savePicture}
                type="button"
              >
                {savingPicture ? "Cuvam..." : "Sacuvaj sliku"}
              </button>
            )}
          </div>
          {pictureChanged && (
            <p className="muted">Nova slika je ucitana. Sacuvaj je da bi se prikazala u profilu.</p>
          )}
        </section>

        <section className="panel">
          <div className="section-head">
            <div>
              <p className="eyebrow">Licni podaci</p>
              <h2>Osnovne informacije</h2>
            </div>
            {!editingProfile && (
              <button className="ghost slim" onClick={() => setEditingProfile(true)} type="button">
                Izmeni
              </button>
            )}
          </div>

          {!editingProfile ? (
            <div className="profile-read-grid">
              <span>
                <small>Ime</small>
                <strong>{form.name || "-"}</strong>
              </span>
              <span>
                <small>Prezime</small>
                <strong>{form.lastName || "-"}</strong>
              </span>
              <span>
                <small>Korisnicko ime</small>
                <strong>{form.username || "-"}</strong>
              </span>
              <span>
                <small>Email</small>
                <strong>{form.email || "-"}</strong>
              </span>
            </div>
          ) : (
            <form className="stack compact-gap" onSubmit={saveProfile}>
              <div className="two-col">
                <label>
                  Ime
                  <input
                    value={form.name}
                    onChange={(e) => setForm((c) => ({ ...c, name: e.target.value }))}
                  />
                </label>
                <label>
                  Prezime
                  <input
                    value={form.lastName}
                    onChange={(e) => setForm((c) => ({ ...c, lastName: e.target.value }))}
                  />
                </label>
                <label className="wide">
                  Korisnicko ime
                  <input
                    value={form.username}
                    onChange={(e) => setForm((c) => ({ ...c, username: e.target.value }))}
                  />
                </label>
                <label className="wide">
                  Email
                  <input
                    type="email"
                    value={form.email}
                    onChange={(e) => setForm((c) => ({ ...c, email: e.target.value }))}
                  />
                </label>
              </div>
              <span className="button-pair">
                <button className="primary" disabled={savingProfile} type="submit">
                  {savingProfile ? "Cuvam..." : "Sacuvaj izmene"}
                </button>
                <button className="ghost" onClick={resetProfileEdit} type="button">
                  Odustani
                </button>
              </span>
            </form>
          )}
          {profileNotice && <p className="notice compact-notice">{profileNotice}</p>}
        </section>

        <section className="panel">
          <div className="section-head">
            <div>
              <p className="eyebrow">Bezbednost</p>
              <h2>Promena lozinke</h2>
            </div>
            {!editingPassword && (
              <button className="ghost slim" onClick={() => setEditingPassword(true)} type="button">
                Izmeni
              </button>
            )}
          </div>

          {!editingPassword ? (
            <p className="muted">Lozinka se ne prikazuje iz bezbednosnih razloga.</p>
          ) : (
            <form className="stack compact-gap" onSubmit={savePassword}>
              <label>
                Trenutna lozinka
                <input
                  autoComplete="current-password"
                  type="password"
                  value={passwordForm.currentPassword}
                  onChange={(e) =>
                    setPasswordForm((c) => ({ ...c, currentPassword: e.target.value }))
                  }
                />
              </label>
              <label>
                Nova lozinka
                <input
                  autoComplete="new-password"
                  type="password"
                  value={passwordForm.newPassword}
                  onChange={(e) =>
                    setPasswordForm((c) => ({ ...c, newPassword: e.target.value }))
                  }
                />
              </label>
              <label>
                Potvrdi novu lozinku
                <input
                  autoComplete="new-password"
                  type="password"
                  value={passwordForm.confirmPassword}
                  onChange={(e) =>
                    setPasswordForm((c) => ({ ...c, confirmPassword: e.target.value }))
                  }
                />
              </label>
              <span className="button-pair">
                <button className="primary" disabled={savingPassword} type="submit">
                  {savingPassword ? "Cuvam..." : "Sacuvaj izmene"}
                </button>
                <button className="ghost" onClick={resetPasswordEdit} type="button">
                  Odustani
                </button>
              </span>
            </form>
          )}
          {passwordNotice && <p className="notice compact-notice">{passwordNotice}</p>}
        </section>
      </div>
    </div>
  );
}
