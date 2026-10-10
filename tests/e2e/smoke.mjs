import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import assert from 'node:assert/strict';

const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1100 }, serviceWorkers: 'block' });
page.setDefaultTimeout(20000);
const errors = [];
const screenshots = process.env.RUTASMART_SCREENSHOTS ?? '.local/screenshots';
let report;
let fuelReport;
await page.route('**/api/v1/trips/*/fuel-plan', async route => {
  const response = await route.fetch({ timeout: 45000 });
  if (response.status() === 201) fuelReport = await response.json();
  await route.fulfill({ response });
});
await page.route('**/api/v1/trips/analyze', async route => {
  const response = await route.fetch();
  if (response.status() === 201) report = await response.json();
  await route.fulfill({ response });
});
page.on('pageerror', error => errors.push(error.message));
await mkdir(screenshots, { recursive: true });
try {
  const navigation = await page.goto(process.env.RUTASMART_URL ?? 'http://localhost:5080', { waitUntil: 'networkidle' });
  assert.equal(navigation.status(),200);
  await page.locator('flt-semantics-placeholder').waitFor({ state: 'attached', timeout: 30000 });
  // Flutter's public accessibility opt-in exposes semantics to browser tests.
  const placeholder = page.locator('flt-semantics-placeholder');
  if (await placeholder.count()) await placeholder.evaluate(element => element.click());
  await page.getByText('Tu viaje empieza acá.', { exact: true }).waitFor({ timeout: 30000 });
  await page.screenshot({ path: `${screenshots}/planner-desktop.png` });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: `${screenshots}/planner-phone.png` });
  await page.setViewportSize({ width: 1440, height: 1100 });
  await page.getByRole('textbox', { name: 'Combustible inicial (L)' }).fill('10');
  await page.getByRole('checkbox', { name: 'Confirmé los puntos del viaje' }).click();
  const responsePromise = page.waitForResponse(response => response.request().method() === 'POST' && response.url().endsWith('/trips/analyze'), { timeout: 30000 });
  await page.getByText('Analizar viaje', { exact: true }).click();
  const response = await responsePromise;
  assert.equal(response.status(), 201);
  assert.ok(report);
  assert.equal(report.route.synthetic, false);
  assert.ok(report.route.distanceM > 250000 && report.route.distanceM < 400000);
  assert.notEqual(report.status, 'favorable');
  await page.getByText('Logo de RutaSmart Informe del viaje', { exact: true }).waitFor();
  await page.waitForTimeout(1200);
  await page.screenshot({ path: `${screenshots}/report-desktop.png` });
  await page.getByText('Calcular plan de carga', { exact: true }).click();
  await page.getByText('Usar este plan y recalcular informe', { exact: true }).waitFor({ timeout: 45000 });
  assert.equal(fuelReport.fuelPlan.status, 'ready');
  assert.ok(fuelReport.fuelPlan.stops.length > 0);
  const acceptedPromise=page.waitForResponse(response=>response.url().endsWith('/fuel-plan') && response.request().method()==='POST', {timeout:45000});
  await page.getByText('Usar este plan y recalcular informe', { exact: true }).click();
  assert.equal((await acceptedPromise).status(),201);
  assert.equal(fuelReport.fuelPlan.status,'accepted');
  await page.mouse.move(1320, 600);
  await page.mouse.wheel(0, -20000);
  await page.waitForTimeout(500);
  await page.mouse.wheel(0, 1000);
  await page.waitForTimeout(800);
  await page.screenshot({ path: `${screenshots}/fuel-plan-desktop.png` });
  await page.getByText('Guardar en este dispositivo', { exact: true }).click();
  await page.getByText('Guardado en este dispositivo', { exact: true }).waitFor();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.mouse.move(190, 300);
  await page.mouse.wheel(0, -20000);
  // Allow the save confirmation to disappear before capturing the actual screen.
  await page.waitForTimeout(4500);
  await page.screenshot({ path: `${screenshots}/report-phone.png` });
  await page.getByText('Volver a planificar', { exact: true }).click();
  await page.setViewportSize({ width: 1440, height: 1100 });
  await page.getByText('Fuentes', { exact: true }).click();
  await page.getByText('Fuentes y cobertura', { exact: true }).waitFor();
  await page.waitForTimeout(1200);
  await page.screenshot({ path: `${screenshots}/sources-desktop.png` });
  assert.deepEqual(errors, []);
  console.log(JSON.stringify({ passed: true, provider: report.route.provider, distanceM: report.route.distanceM,
    weatherMatches: report.weatherAlerts?.length, stationCandidates: report.stations?.length, status: report.status }));
} catch (error) {
  await mkdir('.local/screenshots', { recursive: true });
  await page.screenshot({ path: '.local/screenshots/failure.png' });
  console.error(JSON.stringify({ errors, title: await page.title(), body: (await page.locator('body').innerText()).slice(0,1000) }));
  throw error;
} finally {
  await browser.close();
}
