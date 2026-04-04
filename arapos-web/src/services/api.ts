
import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7229",
});

api.interceptors.request.use((config) => {
  const userToken = localStorage.getItem("token");
  const tenantToken = localStorage.getItem("tenantToken");

  if (userToken) {
    config.headers.Authorization = `Bearer ${userToken}`;
  }

  if (tenantToken) {
    config.headers["X-Tenant-Token"] = tenantToken;
  }

  return config;
});

export default api;




