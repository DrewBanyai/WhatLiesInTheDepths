// usage: node render.js jobs.json outdir   — jobs: [{path, svg, transparent}]
const { chromium } = require('playwright'); const fs=require('fs'); const path=require('path');
(async()=>{
  const jobs=JSON.parse(fs.readFileSync(process.argv[2],'utf8')), out=process.argv[3];
  const b=await chromium.launch(); const p=await b.newPage({deviceScaleFactor:1});
  for (const j of jobs){
    const m=j.svg.match(/width="(\d+)" height="(\d+)"/); const w=+m[1], h=+m[2];
    await p.setViewportSize({width:w,height:h});
    await p.setContent(`<html><body style="margin:0;background:transparent">${j.svg}</body></html>`);
    const f=path.join(out,j.path); fs.mkdirSync(path.dirname(f),{recursive:true});
    await p.locator('svg').first().screenshot({path:f, omitBackground:true});
  }
  await b.close(); console.log('rendered',jobs.length);
})();
