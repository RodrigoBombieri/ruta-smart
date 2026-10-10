// Opt-in live validation. Sequential requests; no tokens or full journeys logged.
import { mkdir, writeFile } from 'node:fs/promises';
const base=process.env.RUTASMART_URL ?? 'http://localhost:5080';
const cases=[
  ['Centro',[-34.6037,-58.3816],[-31.4167,-64.1833],600,850],
  ['Cuyo',[-32.89,-68.84],[-31.5375,-68.5364],140,230],
  ['NOA',[-24.7821,-65.4232],[-24.1858,-65.2995],70,140],
  ['NEA',[-27.3621,-55.9009],[-25.5972,-54.5786],240,370],
  ['Patagonia norte',[-38.9516,-68.0591],[-41.1335,-71.3103],380,550],
  ['Patagonia sur',[-45.8641,-67.4822],[-51.623,-69.2168],700,1000],
  ['Tierra del Fuego',[-54.8019,-68.303],[-53.7877,-67.7095],180,270],
  ['Tránsito internacional',[-51.623,-69.2168],[-54.8019,-68.303],500,800],
];
const results=[];
let token;
for(let i=0;i<cases.length;i++) {
  if(i%4===0) {
    const r=await fetch(`${base}/api/v1/sessions/anonymous`,{method:'POST'});
    if(r.status!==201) throw new Error(`Session: ${r.status}`);
    token=(await r.json()).token;
  }
  const [name,a,b,min,max]=cases[i];
  const location=(label,p)=>({label,point:{latitude:p[0],longitude:p[1]}});
  const body={origin:location(`${name} origen`,a),destination:location(`${name} destino`,b),
    departureAt:new Date(Date.now()+86400000-10800000).toISOString().replace('Z','-03:00'),
    timeZone:'America/Argentina/Buenos_Aires',vehicle:{name:'Validación',fuelType:'nafta_2',tankLitres:47,consumptionLPer100Km:7.5},
    initialLitres:30,reserveLitres:5,manualPriceArsPerLitre:null,pauses:[],demo:false};
  const start=Date.now();
  const response=await fetch(`${base}/api/v1/trips/analyze`,{method:'POST',headers:{Authorization:`Bearer ${token}`,'Content-Type':'application/json','Idempotency-Key':`national-${crypto.randomUUID()}`},body:JSON.stringify(body)});
  const data=await response.json();
  const km=data.route?.distanceM/1000;
  const result={name,status:response.status,km,seconds:(Date.now()-start)/1000,
    roadReferences:data.route?.roads?.length,notices:data.roadNotices?.length,
    international:data.route?.crossesInternationalBorder,
    passed:response.status===201 && km>=min && km<=max && data.route?.synthetic===false && (i!==7 || data.route.crossesInternationalBorder===true)};
  if(!result.passed) result.error=data.title ?? 'Outside expected route range or missing border flag';
  results.push(result); console.log(JSON.stringify(result));
  await new Promise(resolve=>setTimeout(resolve,1200));
}
await mkdir('.local',{recursive:true});
await writeFile('.local/national-routing.json',JSON.stringify({checkedAt:new Date().toISOString(),results},null,2));
if(results.some(r=>!r.passed)) process.exitCode=1;
