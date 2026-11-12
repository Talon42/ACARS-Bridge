namespace A220Compatibility { internal static class DebugScripts {
internal const string Attach = @"(function(){
var key='__dvaAcarsDebugTelemetry',owner=OWNER;
var old=window[key];if(old){if(old.owner===owner)return 'attached';if(Date.now()-old.lastPoll<10000)return 'busy';old.stop();}
if(typeof RegisterViewListener!=='function'||typeof SimVar!=='object')throw new Error('Cockpit APIs unavailable');
var p={owner:owner,engine:{},fuel:{},engineTime:0,lastPoll:Date.now(),engineMessages:0,fuelMessages:0,errors:[]};
p.listener=RegisterViewListener('JS_LISTENER_COMM_BUS');
p.onEngine=function(raw){try{Object.assign(p.engine,JSON.parse(raw));p.engineTime=Date.now();p.engineMessages++;}catch(e){p.errors.push(String(e));}};
p.onFuel=function(raw){try{Object.assign(p.fuel,JSON.parse(raw));p.fuelMessages++;}catch(e){p.errors.push(String(e));}};
p.stop=function(){clearInterval(p.watchdog);p.listener.off('A22X.Engine Data',p.onEngine);p.listener.off('A22X.Fuel Data',p.onFuel);if(window[key]===p)delete window[key];return 'removed';};
p.listener.on('A22X.Engine Data',p.onEngine);p.listener.on('A22X.Fuel Data',p.onFuel);window[key]=p;
p.watchdog=setInterval(function(){if(Date.now()-p.lastPoll>10000)p.stop();},1000);
p.listener.call('COMM_BUS_WASM_CALLBACK','A22X.Resync','{}');return 'attached';
})()";
internal const string Read = @"(function(){var p=window.__dvaAcarsDebugTelemetry;if(!p||p.owner!==OWNER)return JSON.stringify({valid:false,reason:'listener missing'});p.lastPoll=Date.now();var e=p.engine,age=Date.now()-p.engineTime;
if(typeof p.fuel.metric!=='boolean'||age<0||age>4000||typeof e.eng1_fuel_flow!=='number'||typeof e.eng2_fuel_flow!=='number')return JSON.stringify({valid:false,reason:'waiting for fresh engine/fuel messages'});
var k=p.fuel.metric?2.2046226218487757:1;
return JSON.stringify({valid:true,epochMs:p.engineTime,n1a:SimVar.GetSimVarValue('L:A22X Engine 1 N1','number'),n2a:SimVar.GetSimVarValue('L:A22X Engine 1 N2','number'),n1b:SimVar.GetSimVarValue('L:A22X Engine 2 N1','number'),n2b:SimVar.GetSimVarValue('L:A22X Engine 2 N2','number'),fuel1:e.eng1_fuel_flow*k,fuel2:e.eng2_fuel_flow*k,egt1:e.eng1_egt,egt2:e.eng2_egt,master1:SimVar.GetSimVarValue('L:A22X Engine 1 Master','Bool')>0.5,master2:SimVar.GetSimVarValue('L:A22X Engine 2 Master','Bool')>0.5,starting1:e.eng1_start===true,starting2:e.eng2_start===true,windmill1:e.eng1_windmill===true,windmill2:e.eng2_windmill===true,engineMessages:p.engineMessages,fuelMessages:p.fuelMessages,metric:p.fuel.metric,errors:p.errors});})()";
internal const string Cleanup = @"(function(){var p=window.__dvaAcarsDebugTelemetry;return p&&p.owner===OWNER?p.stop():'not owned';})()";
} }