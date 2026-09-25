const fs=require('fs');
const t=fs.readFileSync(process.argv[2] || '../../../../Downloads/Hypnic Empire/Game UI/_Whole Screen.html','utf8');
function grab(marker, from){ // marker like 'var A = {'
  const i=t.indexOf(marker, from||0); if(i<0) throw marker;
  let depth=0, j=t.indexOf('{',i);
  for(let k=j;k<t.length;k++){ if(t[k]==='{')depth++; else if(t[k]==='}'){depth--; if(!depth){ return {src:t.slice(j,k+1), end:k}; }}}
}
const out={};
out.G = eval('('+grab('var G = {', t.indexOf('ConstructsMenu')).src+')');
out.M48 = eval('('+grab('var M48 = {').src+')');
out.PG = eval('('+grab('var PG = {').src+')');
out.UG = eval('('+grab('var UG = {').src+')');
out.VG = eval('('+grab('var VG = {').src+')');
out.A = eval('('+grab('var A = {', t.indexOf('act glyphs')).src+')');
// the fullest resource dict
let best=null, from=0;
while(true){ const i=t.indexOf('var R = {',from); if(i<0)break; const g=grab('var R = {',i); const o=eval('('+g.src+')'); if(!best||Object.keys(o).length>Object.keys(best).length) best=o; from=g.end; }
out.R=best;
fs.writeFileSync('existing.json', JSON.stringify(out,null,1));
for (const k in out) console.log(k, Object.keys(out[k]).join(' '));
out.I = eval('('+grab('var I = {').src+')');
fs.writeFileSync('existing.json', JSON.stringify(out,null,1));
console.log('I', Object.keys(out.I).join(' '));
