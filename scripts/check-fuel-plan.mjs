// Explicit live check against the local API; no credentials are written to disk.
import assert from 'node:assert/strict';
import { writeFile } from 'node:fs/promises';
const base=process.env.RUTASMART_URL ?? 'http://localhost:5080';
const session=await fetch(`${base}/api/v1/sessions/anonymous`,{method:'POST'});
assert.equal(session.status,201);
const {token}=await session.json();
async function post(path,body) {
  const response=await fetch(`${base}/api/v1${path}`,{method:'POST',headers:{Authorization:`Bearer ${token}`,'Content-Type':'application/json','Idempotency-Key':crypto.randomUUID()},body:JSON.stringify(body)});
  const data=await response.json(); assert.equal(response.status,201,JSON.stringify(data)); return data;
}
const report=await post('/trips/analyze',{
  origin:{label:'Buenos Aires',point:{latitude:-34.6037,longitude:-58.3816}},
  destination:{label:'Córdoba',point:{latitude:-31.4167,longitude:-64.1833}},
  departureAt:new Date(Date.now()+86400000-10800000).toISOString().replace('Z','-03:00'),
  timeZone:'America/Argentina/Buenos_Aires',vehicle:{name:'Auto',fuelType:'nafta_2',tankLitres:47,consumptionLPer100Km:7.5},
  initialLitres:30,reserveLitres:5,manualPriceArsPerLitre:null,pauses:[],demo:false,
});
const proposed=await post(`/trips/${report.tripId}/fuel-plan`,{expectedVersion:report.version,accept:false});
assert.equal(proposed.fuelPlan.status,'ready',proposed.fuelPlan.message);
assert.ok(proposed.fuelPlan.stops.length>0);
const accepted=await post(`/trips/${report.tripId}/fuel-plan`,{expectedVersion:proposed.version,accept:true});
assert.equal(accepted.fuelPlan.status,'accepted');
assert.equal(accepted.route.distanceM,proposed.fuelPlan.route.distanceM);
assert.equal(accepted.input.pauses.length,accepted.fuelPlan.stops.length);
let fuel=accepted.input.initialLitres;
for(let i=0;i<accepted.route.legs.length;i++) {
  fuel-=accepted.route.legs[i].distanceM*accepted.input.vehicle.consumptionLPer100Km/100000;
  assert.ok(fuel>=accepted.input.reserveLitres-0.000001);
  if(i<accepted.fuelPlan.stops.length) fuel+=accepted.fuelPlan.stops[i].purchaseLitres;
  assert.ok(fuel<=accepted.input.vehicle.tankLitres);
}
const result={checkedAt:new Date().toISOString(),passed:true,distanceKm:accepted.route.distanceM/1000,stops:accepted.fuelPlan.stops.length,
  extraKm:accepted.fuelPlan.extraDistanceM/1000,purchaseLitres:accepted.fuelPlan.purchaseLitres,arrivalLitres:fuel,version:accepted.version};
await writeFile('.local/fuel-plan-check.json',JSON.stringify(result,null,2));
console.log(JSON.stringify(result));
