const localZone = Intl.DateTimeFormat().resolvedOptions().timeZone || "Europe/Istanbul";

const marketRegistry = [
  {
    id:"us-equities",
    label:"U.S. Equities",
    zone:"America/New_York",
    zoneLabel:"New York",
    sessions:[
      { id:"pre", label:"Pre", start:4, end:9.5, kind:"extended" },
      { id:"regular", label:"Regular", start:9.5, end:16, kind:"regular" },
      { id:"after", label:"After", start:16, end:20, kind:"extended" }
    ],
    boundaries:[4,9.5,16,20],
    regularRanges:[[9.5,16]]
  },
  {
    id:"hk-equities",
    label:"Hong Kong Equities",
    zone:"Asia/Hong_Kong",
    zoneLabel:"Hong Kong",
    sessions:[
      { id:"morning", label:"Morning", start:9.5, end:12, kind:"regular" },
      { id:"lunch", label:"Lunch", start:12, end:13, kind:"break" },
      { id:"afternoon", label:"Afternoon", start:13, end:16, kind:"regular" }
    ],
    boundaries:[9.5,12,13,16],
    regularRanges:[[9.5,12],[13,16]]
  }
];

const collapsedMarketIds = new Set();

function getSliderHours(){
  return Number(document.getElementById("timeSlider").value);
}
function getSimulatedDate(){
  return new Date(Date.now() + getSliderHours() * 3600000);
}
function getParts(date,zone){
  const values = new Intl.DateTimeFormat("en-GB",{
    timeZone:zone,
    weekday:"short",
    year:"numeric",month:"2-digit",day:"2-digit",
    hour:"2-digit",minute:"2-digit",second:"2-digit",
    hourCycle:"h23",timeZoneName:"short"
  }).formatToParts(date);
  return values.reduce((out,item)=>{out[item.type]=item.value;return out;},{});
}
function getDecimalHour(date,zone){
  const p=getParts(date,zone);
  return Number(p.hour)+Number(p.minute)/60+Number(p.second)/3600;
}
function getOffsetMinutes(date,zone){
  const p=getParts(date,zone);
  const renderedUtc=Date.UTC(+p.year,+p.month-1,+p.day,+p.hour,+p.minute,+p.second);
  return (renderedUtc-date.getTime())/60000;
}
function modulo24(value){
  return ((value%24)+24)%24;
}
function formatHour(value){
  const normalized=modulo24(value);
  let hours=Math.floor(normalized);
  let minutes=Math.round((normalized-hours)*60);
  if(minutes===60){
    hours=(hours+1)%24;
    minutes=0;
  }
  return `${hours.toString().padStart(2,"0")}:${minutes.toString().padStart(2,"0")}`;
}
function splitRange(start,end){
  const s=modulo24(start),e=modulo24(end);
  if(s===e)return [[0,24]];
  return s<e?[[s,e]]:[[s,24],[0,e]];
}
function segmentHtml(session){
  return splitRange(session.start,session.end).map(([start,end])=>
    `<i class="segment ${session.kind}" style="left:${start/24*100}%;width:${(end-start)/24*100}%"></i>`
  ).join("");
}
function edgeClass(hour){
  const normalized=modulo24(hour);
  if(normalized<1.25)return "left";
  if(normalized>22.75)return "right";
  return "center";
}
function markerHtml(exchangeHour,labelHour){
  const normalized=modulo24(exchangeHour);
  const left=normalized/24*100;
  const positionClass=edgeClass(normalized);
  return `<i class="marker" style="left:${left}%"></i><span class="marker-time ${positionClass}" style="left:${left}%">${formatHour(labelHour)}</span>`;
}
function boundaryHtml(exchangeHour,labelHour){
  const normalized=modulo24(exchangeHour);
  return `<span class="boundary ${edgeClass(normalized)}" style="left:${normalized/24*100}%">${formatHour(labelHour)}</span>`;
}
function axisHtml(labelOffsetHours){
  return [0,6,12,18,24].map(exchangeHour=>{
    const label=formatHour(exchangeHour+labelOffsetHours);
    return `<span style="left:${exchangeHour/24*100}%">${label}</span>`;
  }).join("");
}
function inRanges(hour,ranges){
  return ranges.some(([start,end])=>hour>=start&&hour<end);
}
function isRegularSessionOpen(market,date){
  const currentHour=getDecimalHour(date,market.zone);
  const weekday=new Intl.DateTimeFormat("en-US",{timeZone:market.zone,weekday:"short"}).format(date);
  return !["Sat","Sun"].includes(weekday) && inRanges(currentHour,market.regularRanges);
}
function renderMarket(market,date){
  const exchangeOffset=getOffsetMinutes(date,market.zone);
  const localOffset=getOffsetMinutes(date,localZone);
  const localMinusExchangeHours=(localOffset-exchangeOffset)/60;
  const exchangeNow=getDecimalHour(date,market.zone);
  const localNow=getDecimalHour(date,localZone);
  const open=isRegularSessionOpen(market,date);
  const collapsed=collapsedMarketIds.has(market.id);

  const bars=market.sessions.map(segmentHtml).join("");
  const exchangeBoundaries=market.boundaries.map(hour=>boundaryHtml(hour,hour)).join("");
  const localBoundaries=market.boundaries.map(hour=>boundaryHtml(hour,hour+localMinusExchangeHours)).join("");

  const chips=market.sessions.map(session=>
    `<span class="session-chip"><strong>${session.label}</strong> ${formatHour(session.start)}–${formatHour(session.end)} · Local ${formatHour(session.start+localMinusExchangeHours)}–${formatHour(session.end+localMinusExchangeHours)}</span>`
  ).join("");

  return `<article class="market ${collapsed?"collapsed":""}">
    <button class="market-header" type="button" data-market-id="${market.id}">
      <span class="market-title">${market.label}</span>
      <span class="market-status ${open?"open":"closed"}">${open?"Open":"Closed"}</span>
      <span class="market-toggle">${collapsed?"▸":"▾"}</span>
    </button>

    <div class="market-body">
      <div class="track">
        <div class="track-label">${market.zoneLabel}</div>
        <div class="timeline-wrap">
          <div class="timeline">
            ${bars}
            ${exchangeBoundaries}
            ${markerHtml(exchangeNow,exchangeNow)}
          </div>
          <div class="axis">${axisHtml(0)}</div>
        </div>
      </div>

      <div class="track">
        <div class="track-label">Local</div>
        <div class="timeline-wrap">
          <div class="timeline">
            ${bars}
            ${localBoundaries}
            ${markerHtml(exchangeNow,localNow)}
          </div>
          <div class="axis">${axisHtml(localMinusExchangeHours)}</div>
        </div>
      </div>

      <div class="sessions">${chips}</div>
    </div>
  </article>`;
}
function render(){
  const date=getSimulatedDate();
  const shift=getSliderHours();
  document.getElementById("offsetLabel").textContent=shift===0?"Now":`${shift>0?"+":""}${shift}h`;
  document.getElementById("marketGrid").innerHTML=marketRegistry.map(market=>renderMarket(market,date)).join("");

  document.querySelectorAll(".market-header").forEach(button=>{
    button.addEventListener("click",()=>{
      const id=button.dataset.marketId;
      collapsedMarketIds.has(id)?collapsedMarketIds.delete(id):collapsedMarketIds.add(id);
      render();
    });
  });
}
document.getElementById("timeSlider").addEventListener("input",render);
document.getElementById("resetButton").addEventListener("click",()=>{
  document.getElementById("timeSlider").value=0;
  render();
});
render();
setInterval(()=>{if(getSliderHours()===0)render()},1000);
