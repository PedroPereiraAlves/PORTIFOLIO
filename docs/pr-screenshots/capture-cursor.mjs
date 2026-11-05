import { chromium } from "playwright";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(__dirname, "cursor");
const base = process.env.HUB_CAPTURE_BASE || "http://localhost:5276";
const email = process.env.HUB_CAPTURE_EMAIL;
const password = process.env.HUB_CAPTURE_PASSWORD;
function diaUtc(diasAtras) {
  const d = new Date();
  d.setUTCHours(12, 0, 0, 0);
  d.setUTCDate(d.getUTCDate() - diasAtras);
  return d.toISOString();
}

const amostra = `Date,User,Kind,Model,Max Mode,Input (w/ Cache Write),Input (w/o Cache Write),Cache Read,Output Tokens,Total Tokens,Cost
${diaUtc(4)},pedro@exemplo.com,Included,cursor-grok-4.6,No,1200,800,400,300,2700,-
${diaUtc(3)},pedro@exemplo.com,Usage-based,claude-opus,Yes,100,2200,0,600,2900,$1.25
${diaUtc(2)},pedro@exemplo.com,Included,composer,No,0,900,100,200,1200,-
${diaUtc(1)},pedro@exemplo.com,Usage-based,cursor-grok-4.6,Yes,50,1500,200,400,2150,$0.80
${diaUtc(0)},pedro@exemplo.com,Free,composer,No,0,400,0,80,480,Free
`;

if (!email || !password) {
  console.error("Defina HUB_CAPTURE_EMAIL e HUB_CAPTURE_PASSWORD. Ver README.");
  process.exit(1);
}

fs.mkdirSync(out, { recursive: true });

async function settle(page) {
  await page.evaluate(() => {
    document.querySelectorAll(".reveal").forEach((el) => el.classList.add("is-visible"));
  });
  await page.evaluate(async () => {
    window.scrollTo(0, document.body.scrollHeight);
    await new Promise((r) => setTimeout(r, 250));
    window.scrollTo(0, 0);
    await new Promise((r) => setTimeout(r, 300));
  });
  await page.waitForTimeout(200);
}

async function assertNoHorizontalOverflow(page, expectedWidth) {
  const metrics = await page.evaluate(() => ({
    innerWidth: window.innerWidth,
    scrollWidth: document.documentElement.scrollWidth,
  }));
  if (metrics.innerWidth !== expectedWidth) {
    throw new Error(`Viewport esperado ${expectedWidth}px, obtido innerWidth=${metrics.innerWidth}`);
  }
  if (metrics.scrollWidth > expectedWidth) {
    throw new Error(`Overflow horizontal: scrollWidth=${metrics.scrollWidth} > ${expectedWidth}`);
  }
  return metrics;
}

async function shot(page, name, expectedWidth) {
  await settle(page);
  const metrics = await assertNoHorizontalOverflow(page, expectedWidth);
  const file = path.join(out, name);
  await page.screenshot({ path: file, fullPage: true });
  console.log(JSON.stringify({ name, ...metrics }));
}

async function login(page) {
  await page.goto(base + "/Account/Login", { waitUntil: "networkidle" });
  await page.fill("#Email", email);
  await page.fill("#Senha", password);
  await Promise.all([
    page.waitForURL("**/admin/**"),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForLoadState("networkidle");
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    deviceScaleFactor: 1,
    colorScheme: "light",
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const t = msg.text();
    if (/Content Security Policy|Refused to execute inline script/i.test(t)) {
      throw new Error(t);
    }
  });

  await page.addInitScript(() => localStorage.removeItem("pedrohung-theme"));
  await login(page);
  await page.goto(base + "/admin/cursor?periodo=7d", { waitUntil: "networkidle" });
  await page.setInputFiles("#arquivo", {
    name: "uso.csv",
    mimeType: "text/csv",
    buffer: Buffer.from(amostra),
  });
  await Promise.all([
    page.waitForURL("**/admin/cursor**"),
    page.click('button:has-text("Importar CSV")'),
  ]);
  await page.waitForSelector("#grafico-consumo");
  await page.waitForTimeout(500);
  await shot(page, "consumo-desktop-1280.png", 1280);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await shot(page, "consumo-mobile-390.png", 390);

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
