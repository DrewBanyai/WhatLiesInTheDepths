// Renderers reproducing the spec set's placeholder art, 1:1 with _Whole Screen.html.
const LINE = 'fill="none" stroke-linecap="round" stroke-linejoin="round"';
function glyph(inner, vb, sw, size, color){ return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${vb} ${vb}" ${LINE} color="${color}" stroke="${color}" stroke-width="${sw}">${inner}</svg>`; }
const SKY = { d:['#F7F1FA','#E7DCF0'], r:['#F1F3FA','#DDE0F0'], w:['#FAF4EE','#EEE1DC'], g:['#FAF0F4','#EDDCE6'], n:['#E8E1F0','#C9BCDB'] };
function plate(inner, tone){ const s=SKY[tone]||SKY.d;
  return '<svg xmlns="http://www.w3.org/2000/svg" width="864" height="300" viewBox="0 0 432 150" preserveAspectRatio="xMidYMid slice">'+
   '<defs><linearGradient id="p1" x1="0" y1="0" x2="0" y2="1">'+
   '<stop offset="0" stop-color="'+s[0]+'"/><stop offset="1" stop-color="'+s[1]+'"/></linearGradient></defs>'+
   '<rect width="432" height="150" fill="url(#p1)"/>'+
   (tone==='n' ? '<circle cx="368" cy="36" r="15" fill="#F4F0FA"/><circle cx="374" cy="31" r="13" fill="'+s[0]+'"/>' : '<circle cx="368" cy="36" r="17" fill="#FDF7EC"/><circle cx="368" cy="36" r="30" fill="#FBF2E1" opacity=".3"/>')+
   '<path d="M0 96c54-14 92 8 148 3s104-16 148-6 90 10 146 5v52H0Z" fill="#DCD1EA" opacity=".85"/>'+
   '<path d="M0 116c62-10 112 8 176 2s110-10 156-3 74 6 110 2v33H0Z" fill="#C9BADF"/>'+
   '<g transform="translate(155 24) scale(3.2)" stroke="#6E5A8C" stroke-width="1.15" fill="none" stroke-linecap="round" stroke-linejoin="round">'+inner+'</g>'+
   '<path d="M0 134c70-6 128 5 198 1s124-6 244 0v15H0Z" fill="#B7A3D0"/>'+
   '<g fill="#FFFFFF" opacity=".45"><circle cx="72" cy="34" r="2"/><circle cx="118" cy="58" r="1.5"/><circle cx="286" cy="26" r="1.6"/></g></svg>'; }
const TONE = {d:['#F7F1FA','#E5D9EF'], r:['#F1F3FA','#DCDFF0'], g:['#F4F6F2','#DCE6DD'], w:['#FAF4EE','#EDE0DA'], n:['#E6DDEF','#BFAFD5']};
function art(inner, tone){ const s=TONE[tone]||TONE.d;
  return '<svg xmlns="http://www.w3.org/2000/svg" width="472" height="300" viewBox="0 0 236 150" preserveAspectRatio="xMidYMid slice">'+
   '<defs><linearGradient id="a1" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="'+s[0]+'"/><stop offset="1" stop-color="'+s[1]+'"/></linearGradient></defs>'+
   '<rect width="236" height="150" fill="url(#a1)"/>'+(tone==='n' ? '<circle cx="186" cy="34" r="12" fill="#F4F0FA"/><circle cx="191" cy="30" r="10.5" fill="'+s[0]+'"/>' : '<circle cx="186" cy="34" r="13" fill="#FDF7EC"/>')+
   '<path d="M0 96c30-12 52 6 84 3s56-14 82-6 46 8 70 4v53H0Z" fill="#DBD0E9" opacity=".85"/>'+
   '<path d="M0 118c34-9 62 6 96 2s62-9 88-3 34 5 52 2v31H0Z" fill="#C7B8DD"/>'+
   '<g transform="translate(86 32) scale(2.3)" stroke="#6E5A8C" stroke-width="1.2" fill="none" stroke-linecap="round" stroke-linejoin="round">'+inner+'</g>'+
   '<path d="M0 134c44-5 80 4 124 1s78-4 112 0v15H0Z" fill="#B3A0CC"/></svg>'; }
function portrait(inner, dark){
  // dark: the nightmare variant — the same figure on a dusk ground, so the swap reads at a glance.
  const g = dark ? ['#E4DCEE','#B9A9CF'] : ['#F4EEFB','#E4D9F2'], halo = dark ? '#CDBFE0' : '#E7DBF7', hill = dark ? '#A895C2' : '#D5C6E8', ink = dark ? '#3F2F5C' : '#6E5A8C';
  return '<svg xmlns="http://www.w3.org/2000/svg" width="192" height="300" viewBox="0 0 96 150" preserveAspectRatio="xMidYMid slice">'+
   '<defs><linearGradient id="r1" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="'+g[0]+'"/><stop offset="1" stop-color="'+g[1]+'"/></linearGradient></defs>'+
   '<rect width="96" height="150" fill="url(#r1)"/><circle cx="48" cy="64" r="34" fill="'+halo+'"/>'+
   '<path d="M0 120c20-7 34 4 54 1s28-4 42-1v30H0Z" fill="'+hill+'"/>'+
   '<g transform="translate(20 36) scale(2.05)" stroke="'+ink+'" stroke-width="1.5" fill="none" stroke-linecap="round" stroke-linejoin="round">'+inner+'</g></svg>'; }
function mapForm(inner){ return `<svg xmlns="http://www.w3.org/2000/svg" width="192" height="192" viewBox="0 0 48 48" ${LINE} stroke="#6E549F" stroke-width="1.4">${inner}</svg>`; }
module.exports = { glyph, plate, art, portrait, mapForm };
