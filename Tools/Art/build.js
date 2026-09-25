const L=require('./lib.js'), N=require('./new.js'), E=require('./existing.json'), fs=require('fs');
const W='#FFFFFF', jobs=[];
const add=(path,svg)=>jobs.push({path,svg});
for (const [k,v] of Object.entries(N.FOCUS)) add(`Glyphs/Focus/${k}.png`, L.glyph(v,28,1.7,112,W));
for (const [k,v] of Object.entries(N.RES)) add(`Glyphs/Resource/${k}.png`, L.glyph(v,20,1.5,96,W));
for (const [k,c] of Object.entries(N.CONSTRUCT)) {
  add(`Glyphs/Construct/${k}.png`, L.glyph(c.g,28,1.7,96,W));
  add(`Spec/Plate_Construct_${k}.png`, L.plate(c.g,c.tone));
  add(`Spec/Map/${k}.png`, L.mapForm(c.m));
}
for (const [k,v] of Object.entries(N.VISION)) add(`Glyphs/Vision/${k}.png`, L.glyph(v,28,1.5,112,W));
const units = Object.assign({}, N.UNIT, { hollowriders: E.UG.rider.replace(/<circle ([^>]*)\/>/, '<circle $1 opacity=".3"/>') });
for (const [k,v] of Object.entries(units)) {
  add(`Glyphs/Unit/${k}.png`, L.glyph(v,28,1.6,112,W));
  add(`Spec/Portrait_Unit_${k}.png`, L.portrait(v, N.NIGHTMARES.includes(k)));
}
for (const [k,p] of Object.entries(N.PLACE)) {
  add(`Glyphs/Place/${k}.png`, L.glyph(p.g,28,1.6,112,W));
  add(`Spec/Art_Place_${k}.png`, L.art(p.g,p.tone));
}
fs.writeFileSync('jobs.json', JSON.stringify(jobs)); console.log(jobs.length,'jobs');
