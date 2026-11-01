import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const www = path.join(root, "src/PedroHub.Web/wwwroot");
const fixture = path.join(path.dirname(fileURLToPath(import.meta.url)), "fixtures/escritorio.html");

export const AGENTES = [
  ["gideon", "Gideon", "Tech lead", "Central", 0, "trabalhando"],
  ["backend-hub", "Backend Hub", "API e domínio", "Hub", 1, "trabalhando"],
  ["front-hub", "Front Hub", "UI do Hub", "Hub", 2, "trabalhando"],
  ["monitoramento-hub", "Monitoramento Hub", "Saúde e alertas", "Hub", 3, "trabalhando"],
  ["seguranca-hub", "Segurança Hub", "Auth e SSRF", "Hub", 4, "revisando"],
  ["infra-testes-hub", "Infra e Testes Hub", "CI e infra", "Hub", 5, "revisando"],
  ["backend-clinica", "Backend Clinica", "API da clínica", "Clinica", 1, "ocioso"],
  ["front-clinica", "Front Clinica", "UI da clínica", "Clinica", 2, "ocioso"],
  ["seguranca-clinica", "Segurança Clinica", "Auth clínica", "Clinica", 3, "ocioso"],
  ["infra-clinica", "Infra Clinica", "Deploy clínica", "Clinica", 4, "bloqueado"],
  ["design-clinica", "Design Clinica", "UX/UI", "Clinica", 5, "pausado-sem-cota"],
  ["testes-clinica", "Testes Clinica", "QA", "Clinica", 6, "pausado-sem-cota"],
].map(([slug, nome, papel, sala, ordem, status]) => ({
  slug,
  nome,
  papel,
  sala,
  ordem,
  status,
  tarefaAtual: status === "trabalhando" ? "Entrega do mapa" : "—",
  projeto: "PORTIFOLIO",
  haQuantoTempo: "agora",
  desatualizado: false,
  ultimasEntregas: [{ tipo: "commit", titulo: "Ajuste visual do escritório" }],
}));

const MIME = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".png": "image/png",
  ".json": "application/json",
  ".svg": "image/svg+xml",
  ".woff2": "font/woff2",
};

export function startHarness() {
  const server = http.createServer((req, res) => {
    const url = decodeURIComponent((req.url || "/").split("?")[0]);
    if (url === "/admin/escritorio/estado") {
      res.writeHead(200, { "content-type": "application/json; charset=utf-8" });
      res.end(JSON.stringify({ pollingSegundos: 120, agentes: AGENTES }));
      return;
    }
    if (url === "/" || url === "/escritorio") {
      res.writeHead(200, { "content-type": "text/html; charset=utf-8" });
      res.end(fs.readFileSync(fixture));
      return;
    }
    const rel = url.replace(/^\/+/, "");
    const file = path.resolve(www, rel);
    if (!file.startsWith(www + path.sep) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
      res.writeHead(404, { "content-type": "text/plain" });
      res.end("nao encontrado");
      return;
    }
    const ext = path.extname(file);
    res.writeHead(200, { "content-type": MIME[ext] || "application/octet-stream" });
    res.end(fs.readFileSync(file));
  });

  return new Promise((resolve) => {
    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      resolve({
        server,
        url: `http://127.0.0.1:${port}/escritorio`,
      });
    });
  });
}
