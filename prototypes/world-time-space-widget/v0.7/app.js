const cityRegistry = [
  { id:"local", city:"Local", zone:Intl.DateTimeFormat().resolvedOptions().timeZone || "Europe/Istanbul", protected:true },
  { id:"lisbon", city:"Lisbon", zone:"Europe/Lisbon" },
  { id:"istanbul", city:"Istanbul", zone:"Europe/Istanbul" },
  { id:"hong-kong", city:"Hong Kong", zone:"Asia/Hong_Kong" },
  { id:"vancouver", city:"Vancouver", zone:"America/Vancouver" },
  { id:"new-york", city:"New York", zone:"America/New_York" },
  { id:"london", city:"London", zone:"Europe/London" },
  { id:"paris", city:"Paris", zone:"Europe/Paris" },
  { id:"dubai", city:"Dubai", zone:"Asia/Dubai" },
  { id:"singapore", city:"Singapore", zone:"Asia/Singapore" },
  { id:"shanghai", city:"Shanghai", zone:"Asia/Shanghai" },
  { id:"tokyo", city:"Tokyo", zone:"Asia/Tokyo" },
  { id:"sydney", city:"Sydney", zone:"Australia/Sydney" },
  { id:"los-angeles", city:"Los Angeles", zone:"America/Los_Angeles" },
  { id:"sao-paulo", city:"São Paulo", zone:"America/Sao_Paulo" },
  { id:"mexico-city", city:"Mexico City", zone:"America/Mexico_City" },
  { id:"toronto", city:"Toronto", zone:"America/Toronto" },
  { id:"chicago", city:"Chicago", zone:"America/Chicago" },
  { id:"delhi", city:"Delhi", zone:"Asia/Kolkata" },
  { id:"seoul", city:"Seoul", zone:"Asia/Seoul" },
  { id:"taipei", city:"Taipei", zone:"Asia/Taipei" },
  { id:"bangkok", city:"Bangkok", zone:"Asia/Bangkok" },
  { id:"jakarta", city:"Jakarta", zone:"Asia/Jakarta" },
  { id:"cairo", city:"Cairo", zone:"Africa/Cairo" },
  { id:"johannesburg", city:"Johannesburg", zone:"Africa/Johannesburg" }
];

const statutoryHolidayDemo = {
  "Europe/Lisbon":new Set(["01-01","04-25","05-01","06-10","12-25"]),
  "Europe/Istanbul":new Set(["01-01","04-23","05-01","05-19","08-30","10-29"]),
  "Asia/Hong_Kong":new Set(["01-01","05-01","07-01","10-01","12-25"]),
  "America/Vancouver":new Set(["01-01","07-01","12-25"]),
  "America/New_York":new Set(["01-01","07-04","12-25"]),
  "Europe/London":new Set(["01-01","12-25"]),
  "Europe/Paris":new Set(["01-01","05-01","07-14","12-25"]),
  "Asia/Dubai":new Set(["01-01","12-02"]),
  "Asia/Singapore":new Set(["01-01","05-01","08-09","12-25"]),
  "Asia/Shanghai":new Set(["01-01","05-01","10-01"]),
  "Asia/Tokyo":new Set(["01-01","02-11","05-03","11-03"]),
  "Australia/Sydney":new Set(["01-01","01-26","12-25"]),
  "America/Los_Angeles":new Set(["01-01","07-04","12-25"]),
  "America/Sao_Paulo":new Set(["01-01","09-07","12-25"]),
  "America/Mexico_City":new Set(["01-01","09-16"]),
  "America/Toronto":new Set(["01-01","07-01","12-25"]),
  "America/Chicago":new Set(["01-01","07-04","12-25"]),
  "Asia/Kolkata":new Set(["01-26","08-15","10-02"]),
  "Asia/Seoul":new Set(["01-01","03-01","08-15","10-03"]),
  "Asia/Taipei":new Set(["01-01","10-10"]),
  "Asia/Bangkok":new Set(["01-01","04-13","12-05"]),
  "Asia/Jakarta":new Set(["01-01","08-17"]),
  "Africa/Cairo":new Set(["01-07","07-23"]),
  "Africa/Johannesburg":new Set(["01-01","04-27","12-16"])
};

const defaultCityIds=["local","lisbon","istanbul","hong-kong","vancouver","new-york"];
const maxCities=21;
const defaultHeightDip=220;
const cityRowIncrementDip=48;
let visibleCityIds=[...defaultCityIds];
let contextCityId=null;

function sliderHours(){return Number(document.getElementById("timeSlider").value)}
function simulatedDate(){return new Date(Date.now()+sliderHours()*3600000)}
function parts(date,zone){
  const values=new Intl.DateTimeFormat("en-GB",{
    timeZone:zone,weekday:"short",day:"2-digit",month:"2-digit",
    hour:"2-digit",minute:"2-digit",hourCycle:"h23",timeZoneName:"short"
  }).formatToParts(date);
  return values.reduce((out,item)=>{out[item.type]=item.value;return out;},{});
}
function cityById(id){return cityRegistry.find(city=>city.id===id)}
function availableCities(){return cityRegistry.filter(city=>!visibleCityIds.includes(city.id))}
function desiredHeightDip(){
  const visibleRows=Math.ceil(visibleCityIds.length/4);
  return defaultHeightDip+Math.max(0,visibleRows-2)*cityRowIncrementDip;
}
function holiday(date,city){
  const p=parts(date,city.zone);
  return (statutoryHolidayDemo[city.zone]||new Set()).has(`${p.month}-${p.day}`);
}
function announceDesiredHeight(){
  window.parent.postMessage({
    type:"world-time-space:desired-height",
    height:desiredHeightDip()
  },"*");
}
function renderCities(){
  const date=simulatedDate();
  const local=cityById("local");
  const localParts=parts(date,local.zone);
  document.getElementById("localClock").textContent=`${local.zone} · ${localParts.hour}:${localParts.minute}`;

  document.getElementById("cityGrid").innerHTML=visibleCityIds.map(id=>{
    const city=cityById(id);
    const p=parts(date,city.zone);
    const isHoliday=holiday(date,city);
    return `<article class="city-card ${city.protected?"local":""}" data-city-id="${city.id}">
      <span class="holiday-badge ${isHoliday?"holiday":"normal"}" title="${isHoliday?"Local statutory holiday":"Not a local statutory holiday"}">${isHoliday?"HOL":"—"}</span>
      <div class="city-name">${city.city}</div>
      <div class="city-time">${p.hour}:${p.minute}</div>
      <div class="city-footer">
        <span class="city-date">${p.weekday} ${p.day}/${p.month}</span>
        <span class="city-zone">${p.timeZoneName}</span>
      </div>
    </article>`;
  }).join("");

  document.querySelectorAll(".city-card").forEach(card=>{
    card.addEventListener("contextmenu",event=>{
      event.preventDefault();
      event.stopPropagation();
      openContextMenu(event,card.dataset.cityId);
    });
  });

  announceDesiredHeight();
}
function openContextMenu(event,cityId=null){
  contextCityId=cityId;
  const menu=document.getElementById("contextMenu");
  const remove=document.getElementById("contextRemoveCity");
  const city=cityId?cityById(cityId):null;

  remove.hidden=!city || city.protected;

  menu.hidden=false;
  menu.style.left=`${Math.max(6,Math.min(event.clientX,window.innerWidth-menu.offsetWidth-6))}px`;
  menu.style.top=`${Math.max(6,Math.min(event.clientY,window.innerHeight-menu.offsetHeight-6))}px`;
}
function closeContextMenu(){
  document.getElementById("contextMenu").hidden=true;
  contextCityId=null;
}
function openChooser(){
  closeContextMenu();
  const cities=availableCities();

  if(window.parent!==window){
    window.parent.postMessage({
      type:"world-time-space:open-add-city-chooser",
      cities,
      currentCount:visibleCityIds.length,
      maxCities
    },"*");
  }else{
    openStandaloneChooser(cities);
  }
}
function openStandaloneChooser(cities=availableCities()){
  const overlay=document.getElementById("standaloneChooser");
  overlay.hidden=false;
  document.getElementById("standaloneChooserSearch").value="";
  renderStandaloneChooserList(cities);
}
function closeStandaloneChooser(){
  document.getElementById("standaloneChooser").hidden=true;
}
function renderStandaloneChooserList(cities){
  const list=document.getElementById("standaloneChooserList");
  const limit=document.getElementById("standaloneChooserLimit");
  const atCapacity=visibleCityIds.length>=maxCities;

  list.innerHTML=atCapacity
    ? ""
    : cities.map(city=>`<button class="chooser-city" type="button" data-add-city-id="${city.id}">
        ${city.city}
        <small>${city.zone}</small>
      </button>`).join("");

  limit.textContent=atCapacity
    ? `Maximum reached: ${visibleCityIds.length} / ${maxCities} cities`
    : `${visibleCityIds.length} / ${maxCities} cities`;

  document.querySelectorAll("[data-add-city-id]").forEach(button=>{
    button.addEventListener("click",()=>{
      addCity(button.dataset.addCityId);
      closeStandaloneChooser();
    });
  });
}
function addCity(id){
  if(visibleCityIds.length>=maxCities)return;
  if(!cityById(id) || visibleCityIds.includes(id))return;
  visibleCityIds.push(id);
  render();
}
function removeContextCity(){
  if(!contextCityId)return;
  const city=cityById(contextCityId);
  if(!city || city.protected)return;
  visibleCityIds=visibleCityIds.filter(id=>id!==contextCityId);
  closeContextMenu();
  render();
}
function render(){
  const shift=sliderHours();
  document.getElementById("offsetLabel").textContent=shift===0?"Now":`${shift>0?"+":""}${shift}h`;
  renderCities();
}
document.getElementById("worldWidget").addEventListener("contextmenu",event=>{
  if(event.target.closest(".city-card"))return;
  event.preventDefault();
  openContextMenu(event,null);
});
document.getElementById("contextAddCity").addEventListener("click",openChooser);
document.getElementById("contextRemoveCity").addEventListener("click",removeContextCity);
document.getElementById("standaloneChooserClose").addEventListener("click",closeStandaloneChooser);
document.getElementById("standaloneChooserSearch").addEventListener("input",event=>{
  const query=event.target.value.trim().toLowerCase();
  renderStandaloneChooserList(availableCities().filter(city=>
    city.city.toLowerCase().includes(query) || city.zone.toLowerCase().includes(query)
  ));
});
window.addEventListener("message",event=>{
  if(event.data?.type==="world-time-space:add-city"){
    addCity(event.data.cityId);
  }
});
document.addEventListener("click",event=>{
  if(!document.getElementById("contextMenu").contains(event.target))closeContextMenu();
});
document.addEventListener("keydown",event=>{
  if(event.key==="Escape"){
    closeContextMenu();
    closeStandaloneChooser();
  }
});
document.getElementById("timeSlider").addEventListener("input",render);
document.getElementById("resetButton").addEventListener("click",()=>{
  document.getElementById("timeSlider").value=0;
  render();
});
render();
setInterval(()=>{if(sliderHours()===0)render()},1000);
