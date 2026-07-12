import { useState } from "react";
import React from "react";
import { useAuth } from "../context/AuthContext.jsx";

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

function validateLogin(form) {
  if (!form.username.trim()) return "Unesi korisnicko ime.";
  if (!form.password.trim()) return "Unesi lozinku.";
  return "";
}

function validateRegister(form) {
  if (!form.name.trim() || !form.lastName.trim()) return "Ime i prezime su obavezni.";
  if (form.username.trim().length < 3) return "Korisnicko ime mora imati najmanje 3 znaka.";
  if (!/^\S+@\S+\.\S+$/.test(form.email)) return "Unesi ispravan email.";
  if (form.password.length < 6) return "Lozinka mora imati najmanje 6 znakova.";
  return "";
}

export default function AuthPage() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState("login");
  const [loginForm, setLoginForm] = useState(emptyLogin);
  const [registerForm, setRegisterForm] = useState(emptyRegister);
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);

  const isLogin = mode === "login";

  async function handleSubmit(event) {
    event.preventDefault();
    setNotice("");

    const validation = isLogin ? validateLogin(loginForm) : validateRegister(registerForm);
    if (validation) {
      setNotice(validation);
      return;
    }

    setBusy(true);
    try {
      if (isLogin) {
        await login({
          username: loginForm.username.trim(),
          password: loginForm.password
        });
      } else {
        await register({
          ...registerForm,
          name: registerForm.name.trim(),
          lastName: registerForm.lastName.trim(),
          username: registerForm.username.trim(),
          email: registerForm.email.trim()
        });
        setMode("login");
        setRegisterForm(emptyRegister);
        setNotice("Registracija je uspesna. Mozes da se prijavis.");
      }
    } catch (error) {
      setNotice(error.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="auth-screen">
      <section className="auth-panel">
        <div className="brand-row">
          <div className="brand-mark">SA</div>
          <div>
            <p className="eyebrow">Snake Architect</p>
            <h1>{isLogin ? "Prijava" : "Registracija"}</h1>
          </div>
        </div>

        <div className="segmented">
          <button className={isLogin ? "active" : ""} onClick={() => setMode("login")}>
            Login
          </button>
          <button className={!isLogin ? "active" : ""} onClick={() => setMode("register")}>
            Register
          </button>
        </div>

        <form className="stack" onSubmit={handleSubmit}>
          {isLogin ? (
            <>
              <label>
                Korisnicko ime
                <input
                  autoComplete="username"
                  value={loginForm.username}
                  onChange={(event) =>
                    setLoginForm((current) => ({ ...current, username: event.target.value }))
                  }
                />
              </label>
              <label>
                Lozinka
                <input
                  autoComplete="current-password"
                  type="password"
                  value={loginForm.password}
                  onChange={(event) =>
                    setLoginForm((current) => ({ ...current, password: event.target.value }))
                  }
                />
              </label>
            </>
          ) : (
            <div className="stack two-col compact-gap">
              <label>
                Ime
                <input
                  value={registerForm.name}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, name: event.target.value }))
                  }
                />
              </label>
              <label>
                Prezime
                <input
                  value={registerForm.lastName}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, lastName: event.target.value }))
                  }
                />
              </label>
              <label className="wide">
                Korisnicko ime
                <input
                  autoComplete="username"
                  value={registerForm.username}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, username: event.target.value }))
                  }
                />
              </label>
              <label className="wide">
                Email
                <input
                  autoComplete="email"
                  type="email"
                  value={registerForm.email}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, email: event.target.value }))
                  }
                />
              </label>
              <label className="wide">
                Lozinka
                <input
                  autoComplete="new-password"
                  type="password"
                  value={registerForm.password}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, password: event.target.value }))
                  }
                />
              </label>
            </div>
          )}

          <button className="primary" disabled={busy}>
            {busy ? "Sacekaj..." : isLogin ? "Prijavi se" : "Napravi nalog"}
          </button>
        </form>

        {notice && <p className="notice">{notice}</p>}
      </section>
    </main>
  );
}
