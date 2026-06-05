const worldFrame=document.getElementById("worldFrame");
const tradingFrame=document.getElementById("tradingFrame");
const overlay=document.getElementById("addCityOverlay");
const overlaySearch=document.getElementById("overlaySearch");
const overlayCityList=document.getElementById("overlayCityList");
const overlayLimit=document.getElementById("overlayLimit");
let chooserCities=[];
let chooserCurrentCount=0;
let chooserMaxCities=21;

function setWorldHeight(height){
  worldFrame.style.height=`${Math.max(220,Number(height)||220)}px`;
}
function measureTradingHeight(){
  try{
    const doc=tradingFrame.contentDocument;
    if(!doc)return;
    const collapsed=doc.querySelectorAll(".market.collapsed").length;
    const measured=Math.ceil(doc.documentElement.scrollHeight);
    const height=collapsed>0?Math.max(120,measured):Math.max(500,measured);
    tradingFrame.style.height=`${height}px`;
  }catch(error){
    tradingFrame.style.height="500px";
  }
}
function attachTradingObserver(){
  measureTradingHeight();
  try{
    const doc=tradingFrame.contentDocument;
    if(!doc)return;
    doc.addEventListener("click",()=>setTimeout(measureTradingHeight,0));
    new MutationObserver(()=>measureTradingHeight()).observe(doc.body,{
      attributes:true,
      childList:true,
      subtree:true
    });
  }catch(error){
    tradingFrame.style.height="500px";
  }
}
function renderChooser(){
  const query=overlaySearch.value.trim().toLowerCase();
  const atCapacity=chooserCurrentCount>=chooserMaxCities;
  const filtered=chooserCities.filter(city=>
    city.city.toLowerCase().includes(query) || city.zone.toLowerCase().includes(query)
  );

  overlayCityList.innerHTML=atCapacity
    ? ""
    : filtered.map(city=>`<button class="chooser-city" type="button" data-city-id="${city.id}">
        ${city.city}
        <small>${city.zone}</small>
      </button>`).join("");

  overlayLimit.textContent=atCapacity
    ? `Maximum reached: ${chooserCurrentCount} / ${chooserMaxCities} cities`
    : `${chooserCurrentCount} / ${chooserMaxCities} cities`;

  overlayCityList.querySelectorAll("[data-city-id]").forEach(button=>{
    button.addEventListener("click",()=>{
      worldFrame.contentWindow.postMessage({
        type:"world-time-space:add-city",
        cityId:button.dataset.cityId
      },"*");
      closeOverlay();
    });
  });
}
function openOverlay(data){
  chooserCities=data.cities||[];
  chooserCurrentCount=Number(data.currentCount)||0;
  chooserMaxCities=Number(data.maxCities)||21;
  overlaySearch.value="";
  renderChooser();
  overlay.hidden=false;
  overlaySearch.focus();
}
function closeOverlay(){
  overlay.hidden=true;
}
window.addEventListener("message",event=>{
  if(event.data?.type==="world-time-space:desired-height"){
    setWorldHeight(event.data.height);
  }
  if(event.data?.type==="world-time-space:open-add-city-chooser"){
    openOverlay(event.data);
  }
});
overlaySearch.addEventListener("input",renderChooser);
document.getElementById("overlayClose").addEventListener("click",closeOverlay);
overlay.addEventListener("click",event=>{
  if(event.target===overlay)closeOverlay();
});
document.addEventListener("keydown",event=>{
  if(event.key==="Escape")closeOverlay();
});
tradingFrame.addEventListener("load",attachTradingObserver);
