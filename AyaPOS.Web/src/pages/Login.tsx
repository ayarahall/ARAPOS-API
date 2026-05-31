import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../services/api";

export default function Login() {

  const [tenant, setTenant] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleLogin = async () => {
    try {
      const res = await api.post("/auth/login", {
        tenantSlug: tenant,
        username,
        password,
      });

      localStorage.setItem("token", res.data.token);

      navigate("/dashboard");
    } catch (err: any) {
      const msg = err?.response?.data || err?.message || "Login failed";
      alert("Login failed: " + (typeof msg === "object" ? JSON.stringify(msg) : msg));
    }
  };

  return (
    
    <div>
      <h2>Login</h2>

      <input
  placeholder="Tenant"
  onChange={(e) => setTenant(e.target.value)}
/>

      <input
        placeholder="Username"
        onChange={(e) => setUsername(e.target.value)}
      />
      <input
        type="password"
        placeholder="Password"
        onChange={(e) => setPassword(e.target.value)}
      />
      <button onClick={handleLogin}>Login</button>
    </div>
  );
}