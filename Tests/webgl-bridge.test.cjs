const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const base = path.join(__dirname, '../Assets/PluginYourGames/Modules');
const source = relative => fs.readFileSync(path.join(base, relative), 'utf8');
let checks = 0;
function check(ok, name) { assert.ok(ok, name); checks++; console.log('PASS: ' + name); }
function fixture(code, extra = {}) {
 const calls=[],logs=[],timers=new Map();let timerId=0;
 const context={console:{error:(...args)=>logs.push(args.join(' ')),warn:(...args)=>logs.push(args.join(' '))},
  setTimeout:(callback,delay)=>{timers.set(++timerId,{callback,delay});return timerId;},clearTimeout:id=>timers.delete(id),
  NO_DATA:'no data',ysdk:null,player:null,initGame:true,
  LogStyledMessage(){},FocusGame(){},YG2Instance:(...args)=>calls.push(args),...extra};
 vm.createContext(context);vm.runInContext(code,context);return{context,calls,logs,timers};
}
async function run() {
 const cloudCode=source('Storage/Scripts/Editor/CopyCode/CloudStorage_js.js');
 let f=fixture(cloudCode);await f.context.LoadCloud();
 check(f.calls[0][1]==='PYATERKA_CLOUD_UNAVAILABLE','unavailable cloud is not an empty save');
 f=fixture(cloudCode,{ysdk:{},player:{getData:async()=>({})}});await f.context.LoadCloud();
 check(f.calls[0][1]==='no data','new account has no save');
 const saved=JSON.stringify({pyaterkaProgress:JSON.stringify({Coins:35,SelectedCar:2})});
 f=fixture(cloudCode,{ysdk:{},player:{getData:async()=>({saves:[saved]})}});await f.context.LoadCloud();
 check(JSON.parse(f.calls[0][1])[0]===saved,'nested JSON survives SDK transport');
 f=fixture(cloudCode,{ysdk:{},player:{getData:async()=>{throw Error('offline')}}});await f.context.LoadCloud();
 check(f.calls[0][1]==='PYATERKA_CLOUD_UNAVAILABLE','failed cloud request retains local recovery');
 const adCode=source('InterstitialAdv/Scripts/Editor/CopyCode/InterAdv_js.js');
 f=fixture(adCode);f.context.InterAdvShow();check(f.calls[0][0]==='ErrorInterAdv','missing SDK releases ad wait');
 f=fixture(adCode,{ysdk:{adv:{showFullscreenAdv(){throw Error('offline')}}}});f.context.InterAdvShow();
 check(f.calls[0][0]==='ErrorInterAdv','ad exception releases wait');
 let callbacks;
 f=fixture(adCode,{ysdk:{adv:{showFullscreenAdv:arg=>callbacks=arg.callbacks}}});f.context.InterAdvShow();
 callbacks.onOpen();callbacks.onClose(false);
 check(f.calls[0][0]==='OpenInterAdv'&&f.calls[1][0]==='CloseInterAdv'&&f.calls[1][1]==='false','ad close even when not shown');
 f=fixture(adCode,{ysdk:{adv:{showFullscreenAdv:arg=>callbacks=arg.callbacks}}});f.context.InterAdvShow();callbacks.onError(Error());
 check(f.calls[0][0]==='ErrorInterAdv','ad error callback');
 const lbCode=source('Leaderboards/Scripts/Editor/CopyCode/LB_js.js');
 const scores=[];
 f=fixture(lbCode,{ysdk:{isAvailableMethod:async()=>true,leaderboards:{getPlayerEntry:async()=>({score:50}),setScore:async(...args)=>scores.push(args)}}});
 await f.context.SetLeaderboard('BestLivingTime',40);check(scores.length===0,'lower record does not overwrite cloud record');
 await f.context.SetLeaderboard('BestLivingTime',60);check(scores[0][1]===60,'higher record submitted');
 f.context.ysdk.isAvailableMethod=async()=>false;await f.context.SetLeaderboard('BestLivingTime',70);
 check(scores.length===1,'unauthorized leaderboard request skipped');
 const payCode=source('Payments/Scripts/Editor/CopyCode/Payments_js.js');
 const consumed=[];let catalogCalls=0;
 const payments={getCatalog:async()=>{catalogCalls++;return[]},getPurchases:async()=>[{productID:'pyaterka_premium_car',purchaseToken:'token'}],consumePurchase:async token=>consumed.push(token)};
 f=fixture(payCode,{ysdk:{getPayments:async()=>payments,environment:{i18n:{lang:'en'}}}});
 await f.context.InitPayments();await f.context.ConsumePurchases(true);
 check(consumed.length===0,'permanent car entitlement is not consumed');
 check(JSON.parse(f.calls[0][1]).ownedProductIds.includes('pyaterka_premium_car'),'permanent ownership restored from SDK');
 check(!payCode.includes('miner_titan_pickaxe'),'Miner product removed');
 const premium={id:'pyaterka_premium_car',title:'VIP',description:'Car',imageURI:'',price:'99 YAN',priceValue:'99',priceCurrencyCode:'YAN'};
 const sdk = pay => ({getPayments:async()=>pay,environment:{i18n:{lang:'ru'}}});
 f=fixture(payCode,{ysdk:sdk({getCatalog:async()=>[premium],getPurchases:async()=>[]})});
 await f.context.InitPayments();
 check(JSON.parse(f.calls[0][1]).id[0]===premium.id,'catalog works without optional currency icon method');
 check(f.timers.size===0,'successful initialization does not poll');
 f=fixture(payCode,{ysdk:sdk({getCatalog:async()=>[{...premium,getPriceCurrencyImage(){throw Error('icon')}}],getPurchases:async()=>[]})});
 await f.context.InitPayments();
 check(JSON.parse(f.calls[0][1]).price[0]==='99 YAN','currency icon error preserves purchasable catalog');
 let failOwnership=true,reads=0;
 const recovering={getCatalog:async()=>[premium],getPurchases:async()=>{reads++;if(failOwnership)throw Error('temporary offline');return[]}};
 f=fixture(payCode,{ysdk:sdk(recovering)});
 await Promise.all([f.context.InitPayments(),f.context.InitPayments()]);
 check(reads===1,'concurrent initialization shares one SDK request');
 check(JSON.parse(f.calls[0][1]).id.length===0&&f.logs.some(log=>log.includes('getPurchases failed')),'ownership failure blocks charging and reports exact failing request');
 check(f.timers.size===1,'temporary failure schedules one retry');
 failOwnership=false;
 let timer=[...f.timers.values()][0];f.timers.clear();timer.callback();await f.context.InitPayments();
 check(JSON.parse(f.calls.at(-1)[1]).id[0]===premium.id,'retry restores the offer after temporary ownership failure');
 f=fixture(payCode,{ysdk:sdk({getCatalog:async()=>[],getPurchases:async()=>[]})});
 await f.context.InitPayments();
 check(f.logs.some(log=>log.includes('VIP product missing')),'empty catalog is diagnosed instead of silently reporting success');
 for(let i=0;i<3;i++) {const next=[...f.timers.values()][0];f.timers.clear();if(next){next.callback();await f.context.InitPayments();}}
 check(f.timers.size===0,'retry count is bounded');
 f=fixture(payCode,{ysdk:sdk({getCatalog:async()=>{throw Error('catalog offline')},getPurchases:async()=>[{productID:premium.id}]})});
 await f.context.InitPayments();
 check(JSON.parse(f.calls[0][1]).ownedProductIds.includes(premium.id)&&f.logs.some(log=>log.includes('getCatalog failed')),'catalog outage preserves verified permanent ownership and reports failure');
 let charges=0;
 const checkout={getCatalog:async()=>[premium],getPurchases:async()=>[{productID:premium.id}],purchase:async()=>{charges++;return{productID:premium.id}}};
 f=fixture(payCode,{ysdk:sdk(checkout)});await f.context.InitPayments();await f.context.BuyPayments(premium.id);
 check(charges===0&&f.calls.at(-1)[0]==='OnPurchaseSuccess','already owned permanent car restores without another charge');
 checkout.getPurchases=async()=>[];
 f=fixture(payCode,{ysdk:sdk(checkout)});await f.context.InitPayments();await f.context.BuyPayments(premium.id);
 check(charges===1&&f.calls.at(-1)[0]==='OnPurchaseSuccess','unowned car opens SDK checkout and unlocks only after success');
 checkout.getPurchases=async()=>{throw Error('cannot verify ownership')};
 f=fixture(payCode,{ysdk:sdk(checkout)});await f.context.InitPayments();await f.context.BuyPayments(premium.id);
 check(charges===1&&f.calls.at(-1)[0]==='OnPurchaseFailed','ownership lookup failure cannot charge an already owned car');
 console.log('Passed '+checks+' YG2 JavaScript checks');
}
run().catch(error=>{console.error(error);process.exitCode=1});
