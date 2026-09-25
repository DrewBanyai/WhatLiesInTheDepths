// New placeholder art for What Lies In The Depths, drawn in the spec set's own idiom:
// 28-grid line glyphs, 48-grid palace forms with pale fills, composited by the spec's plate,
// place-art and portrait templates (lib.js). Keys match the ids the content will use.

// ---- Focus tasks (28 grid) -------------------------------------------------------------
const FOCUS = {
  // Still the Mind: a moon over water gone flat
  still: '<circle cx="14" cy="9.5" r="4.2"/><path d="M4 18h20"/><path d="M8 22h12"/><path d="M11.5 25.5h5"/>',
  // Kindle: a small flame over crossed sticks
  kindle: '<path d="M14 3.5c2.6 3 4.6 5.3 4.6 8.2a4.6 4.6 0 0 1-9.2 0c0-2 1.2-3.3 2.2-4.4.3 1.6 1 2.4 2 2.8-.4-2.4-.2-4.4.4-6.6Z"/><path d="M5.5 24.5 22.5 19M5.5 19l17 5.5"/>',
  // Let It Out: a tear falling into a dish
  weep: '<path d="M14 3.5c2.4 3.4 4.2 5.6 4.2 8a4.2 4.2 0 0 1-8.4 0c0-2.4 1.8-4.6 4.2-8Z"/><path d="M5 20.5h18"/><path d="M7 20.5c.6 2.6 3.4 4 7 4s6.4-1.4 7-4"/>',
  // Stand the Watch: a crenellated wall with an open eye in it
  watch: '<path d="M7.5 25V11h13v14"/><path d="M7.5 11V6.5h3V9h2.2V6.5h2.6V9h2.2V6.5h3V11"/><path d="M10.4 18c2-2.4 5.2-2.4 7.2 0-2 2.4-5.2 2.4-7.2 0Z"/><path d="M4 25h20"/>',
  // Rally: a banner, and the sound it carries
  rally: '<path d="M8 25V3.5"/><path d="M8 4.5h12.5l-3 4 3 4H8"/><path d="M4.5 25h7"/><path d="M19.5 17c1.4 1.4 1.4 3.4 0 4.8M22.5 15c2.6 2.6 2.6 6.2 0 8.8"/>',
  // Temper: a hammer over the anvil
  temper: '<path d="M4.5 14h13.5c0 2.4 1.8 3.6 4 3.6v1.6H13l1.2 3.2h2.3v2.1h-9v-2.1h2.3L11 19.2H7.3A2.8 2.8 0 0 1 4.5 16.4Z"/><path d="M16.4 5.2 19.8 2l3.3 3.4-3.3 3.3Z"/><path d="M18.2 7.1 13 12.2"/>',
  // Sing Together: two notes beamed
  sing: '<circle cx="7.4" cy="20.5" r="2.6"/><circle cx="19.4" cy="17.5" r="2.6"/><path d="M10 20.5V7.2l12-3v13.3"/><path d="M10 11.6l12-3"/>',
  // Dredge the Nightmares: a hook going down into dark water
  dredge: '<path d="M14 2.5v12.2a3.6 3.6 0 0 1-7.2 0V13"/><path d="M5.2 14.2 6.8 12l1.6 2.2"/><path d="M3 20c2.2-1.8 4.4-1.8 6.6 0s4.4 1.8 6.6 0 4.4-1.8 6.6 0"/><path d="M3 24.5c2.2-1.8 4.4-1.8 6.6 0s4.4 1.8 6.6 0 4.4-1.8 6.6 0"/>',
  // Let It Go: open hands, something small leaving them
  release: '<path d="M4 16.5c3 4.4 6.6 6.8 10 6.8s7-2.4 10-6.8"/><path d="M7.8 17.4c2 2 4 3 6.2 3s4.2-1 6.2-3"/><path d="M10.2 10.2c1.5-1.6 2.7-1.6 3.8 0 1.1-1.6 2.3-1.6 3.8 0"/><path d="M14.6 5.2c.9-1 1.7-1 2.4 0 .7-1 1.5-1 2.4 0"/>'
};

// ---- Resources (20 grid) ---------------------------------------------------------------
const RES = {
  // Echo: your voice, and the same shape coming back
  echo: '<path d="M6.2 5.8c-2.2 2.3-2.2 6.1 0 8.4"/><path d="M9 8c-1 1.1-1 2.9 0 4"/><path d="M13.8 5.8c2.2 2.3 2.2 6.1 0 8.4" opacity=".55"/><path d="M11 8c1 1.1 1 2.9 0 4" opacity=".55"/>',
  // Mettle: an ingot with a spark over it
  mettle: '<path d="M3.2 15.5h13.6l-2.4-6H5.6Z"/><path d="M10 3v2.6M6 4.4l1.3 1.9M14 4.4l-1.3 1.9"/>',
  // Umbra: an eclipse
  umbra: '<circle cx="10" cy="10" r="6.6"/><path d="M10 3.4a6.6 6.6 0 0 0 0 13.2 9 9 0 0 1 0-13.2Z" fill="currentColor"/>'
};

// ---- Constructs: card glyph (28 grid) and palace form (48 grid), with the plate's tone ---
const CONSTRUCT = {
  winch: { tone: 'w',
    g: '<path d="M6 24 11 5.5h6L22 24"/><circle cx="14" cy="9" r="3"/><path d="M14 12v7.5"/><path d="M11.4 19.5h5.2v3.4h-5.2Z"/><path d="M3.5 24h21"/>',
    m: '<path d="M10 41 18.5 9h11L38 41"/><circle cx="24" cy="14" r="5.5" fill="#FCFAFE"/><circle cx="24" cy="14" r="1.2"/>' +
       '<path d="M24 19.5v10"/><path d="M20 29.5h8l-1 6h-6Z" fill="#F1EAF9"/><path d="M14.4 27h19.2" opacity=".5"/>' +
       '<path d="M4 41h40"/>' },
  scriptorium: { tone: 'w',
    g: '<path d="M4 12 14 5l10 7"/><path d="M6 11v13h16V11"/><path d="M9.5 20c1.6-1 3-1 4.5 0 1.5-1 2.9-1 4.5 0v-5c-1.6-1-3-1-4.5 0-1.5-1-2.9-1-4.5 0Z"/><path d="M14 15v5"/>',
    m: '<path d="M24 8 42 21H6Z" fill="#F1EAF9"/><path d="M10 21h28v20H10Z" fill="#FCFAFE"/>' +
       '<path d="M17 34c2.4-1.5 4.6-1.5 7 0 2.4-1.5 4.6-1.5 7 0v-7.5c-2.4-1.5-4.6-1.5-7 0-2.4-1.5-4.6-1.5-7 0Z" fill="#F3EDFA"/><path d="M24 26.5V34"/>' +
       '<path d="M35.5 13.5c2-3 4-4.4 6-5-1 2.8-2.8 4.8-5.2 6.2Z" opacity=".7"/><path d="M6 21h36"/><path d="M4 41h40"/>' },
  saltpans: { tone: 'w',
    g: '<path d="M3.5 17h21"/><path d="M5 17v3.5h7.5V17M15.5 17v3.5H23V17"/><path d="M6.8 17l2-4 2 4M17.2 17l2-5 2 5"/><path d="M19.2 8V5.5"/><path d="M3.5 24.5h21"/>',
    m: '<path d="M5 33h17v5H5Z" fill="#F3F0FB"/><path d="M26 33h17v5H26Z" fill="#F3F0FB"/>' +
       '<path d="M9 33l4.5-7 4.5 7Z" fill="#FCFAFE"/><path d="M30 33l4.5-9 4.5 9Z" fill="#FCFAFE"/>' +
       '<path d="M22 11v22" opacity=".6"/><path d="M19 11h6" opacity=".6"/><path d="M34.5 19v-3M13.5 22v-3" opacity=".4"/>' +
       '<path d="M3 41h42"/>' },
  nightlight: { tone: 'g',
    g: '<path d="M9.2 9.5h9.6l2.6 7H6.6Z"/><path d="M14 16.5v6"/><path d="M10 22.5h8"/><path d="M14 9.5V7"/><path d="M5.5 5.5 7.4 7.4M22.5 5.5l-1.9 1.9M14 2.5v1.8"/>',
    m: '<path d="M10 11a17 17 0 0 1 28 0" opacity=".3"/><path d="M14 15a12 12 0 0 1 20 0" opacity=".5"/>' +
       '<path d="M17.5 15h13l4.5 11H13Z" fill="#FCFAFE"/><path d="M24 26v9"/><path d="M17 35h14v6H17Z" fill="#F1EAF9"/>' +
       '<path d="M4 41h40"/>' },
  belltower: { tone: 'w',
    g: '<path d="M8 25V9l6-5 6 5v16"/><path d="M14 10.5a3 3 0 0 0-3 3V16l-1 1.5h8L17 16v-2.5a3 3 0 0 0-3-3Z"/><path d="M11.8 25v-4h4.4v4"/><path d="M4.5 25h19"/>',
    m: '<path d="M24 4 33 13H15Z" fill="#F1EAF9"/><path d="M16 13h16v28H16Z" fill="#FCFAFE"/>' +
       '<path d="M24 16.5a4.5 4.5 0 0 0-4.5 4.5v3.5l-1.5 2h12l-1.5-2V21a4.5 4.5 0 0 0-4.5-4.5Z" fill="#F3EDFA"/><path d="M22.5 27.8a1.5 1.5 0 0 0 3 0"/>' +
       '<path d="M21 41v-7h6v7"/><path d="M24 4V1.5"/><path d="M36 20c1.6 1.6 1.6 4 0 5.6M39 17.5c3 3 3 7.6 0 10.6" opacity=".45"/><path d="M4 41h40"/>' },
  forge: { tone: 'w',
    g: '<path d="M4.5 14h13.5c0 2.4 1.8 3.6 4 3.6v1.6H13l1.2 3.2h2.3v2.1h-9v-2.1h2.3L11 19.2H7.3A2.8 2.8 0 0 1 4.5 16.4Z"/><path d="M19.6 3.5c1.8 2 3 3.4 3 5.2a3 3 0 0 1-6 0c0-1.4 1-2.4 1.6-3.2.2 1 .7 1.6 1.4 1.8-.2-1.4 0-2.6 0-3.8Z"/>',
    m: '<path d="M33 15.5V7h5v11" fill="#F1EAF9"/><path d="M35.5 5c1.5-1.4.5-2.8 0-3.6" opacity=".6"/>' +
       '<path d="M6 41V21l18-9 18 9v20Z" fill="#FCFAFE"/><path d="M17.5 41V31a6.5 6.5 0 0 1 13 0v10Z" fill="#FBF0DC"/>' +
       '<path d="M22 36h4" opacity=".6"/><path d="M3 41h42"/>' },
  hallmany: { tone: 'd',
    g: '<path d="M3 12.5 14 6l11 6.5"/><path d="M5 11.5V24h18V11.5"/><path d="M7.4 24v-5a2 2 0 0 1 4 0v5M12 24v-6a2 2 0 0 1 4 0v6M16.6 24v-5a2 2 0 0 1 4 0v5"/>',
    m: '<path d="M24 8 45 20H3Z" fill="#F1EAF9"/><path d="M6 20h36v21H6Z" fill="#FCFAFE"/>' +
       '<path d="M10 41v-8a3 3 0 0 1 6 0v8M21 41v-9a3 3 0 0 1 6 0v9M32 41v-8a3 3 0 0 1 6 0v8"/><path d="M12 26h2M23 25h2M34 26h2" opacity=".5"/>' +
       '<path d="M12 9.5V4h3.5l-1.2 1.4 1.2 1.4H12M33 9.5V4h3.5l-1.2 1.4 1.2 1.4H33" opacity=".6"/><path d="M3 20h42"/><path d="M2 41h44"/>' },
  hallstrengths: { tone: 'w',
    g: '<path d="M3.5 10 14 4.5 24.5 10"/><path d="M5 10h18"/><path d="M7.5 10v12M12 10v12M16 10v12M20.5 10v12"/><path d="M4.5 22h19"/><path d="M3.5 25h21"/>',
    m: '<path d="M24 6 43 16H5Z" fill="#F1EAF9"/><path d="M6 16h36"/><path d="M11 16v21M19 16v21M29 16v21M37 16v21"/>' +
       '<path d="M8 37h32v4H8Z" fill="#F3EDFA"/>' +
       '<path d="M24 21.5l1.6 3.3 3.6.5-2.6 2.5.6 3.6-3.2-1.7-3.2 1.7.6-3.6-2.6-2.5 3.6-.5Z" fill="#FCFAFE"/><path d="M3 41h42"/>' },
  beacon: { tone: 'g',
    g: '<path d="M10.5 25 12 11h4l1.5 14"/><path d="M11 8h6v3h-6Z"/><path d="M14 8V5.5"/><path d="M6.5 7 9 8.2M21.5 7 19 8.2M6.5 11.8 9 10.8M21.5 11.8 19 10.8"/><path d="M4 25h20"/><path d="M12.5 17.5h3" opacity=".6"/>',
    m: '<path d="M13 12 6 9.5M13 16l-7 2.5M35 12l7-2.5M35 16l7 2.5" opacity=".45"/>' +
       '<path d="M18 41 20.5 17h7L30 41Z" fill="#FCFAFE"/><path d="M19.5 12h9v5h-9Z" fill="#FBF0DC"/><path d="M18 12h12"/>' +
       '<path d="M24 12V8.5"/><path d="M21 8.5h6l-3-3.5Z" fill="#F1EAF9"/><path d="M19.4 26h9.2M18.5 33h11" opacity=".5"/><path d="M4 41h40"/>' },
  kiln: { tone: 'n',
    g: '<path d="M5 24.5V17a9 9 0 0 1 18 0v7.5"/><path d="M10.5 24.5v-4a3.5 3.5 0 0 1 7 0v4"/><path d="M3.5 24.5h21"/><path d="M17.6 7.6c-2.6-1.4-2.6-3.8 0-5.2M13.4 7.8c-1.8-1-1.8-2.6 0-3.6"/>',
    m: '<path d="M30 12c-3-2-3-5 0-7M25 11.5c-2-1.4-2-3.4 0-4.8" opacity=".6"/>' +
       '<path d="M8 41V29a16 16 0 0 1 32 0v12Z" fill="#E4DCEE"/><path d="M18 41v-7a6 6 0 0 1 12 0v7Z" fill="#4A3A66"/>' +
       '<path d="M11 31h5M32 31h5M14 24.5h5M29 24.5h5" opacity=".45"/><path d="M4 41h40"/>' }
};

// ---- Visions (28 grid) -----------------------------------------------------------------
const VISION = {
  // The Other Tenant: a bed, and an eye open above it
  tenant: '<path d="M3 21.5h22"/><path d="M4.5 21.5V15h5a3 3 0 0 1 3 3v3.5"/><path d="M12.5 18h11.5v3.5"/><path d="M3 21.5v3M25 21.5v3"/><path d="M8 8.5c3.4-4 8.6-4 12 0-3.4 4-8.6 4-12 0Z"/><circle cx="14" cy="8.5" r="1.6"/>',
  // What I Could Wake: a crescent, and claws coming out of it
  wake: '<path d="M17.8 3.2a10.5 10.5 0 1 0 7 12.4 8.4 8.4 0 0 1-7-12.4Z"/><path d="M8.6 12.6l2.2 5.2M12.4 11.6l1.2 6.2M16.2 12.4l.2 5.4"/>',
  // The Night I Looked: the light switch, turned on
  looked: '<path d="M9 4h10a1.5 1.5 0 0 1 1.5 1.5v17A1.5 1.5 0 0 1 19 24H9a1.5 1.5 0 0 1-1.5-1.5v-17A1.5 1.5 0 0 1 9 4Z"/><path d="M12 9.5h4v7h-4Z"/><path d="M12 9.5l4 3"/><path d="M3.2 8.6 5.2 9.6M3 14h2.2M24.8 8.6l-2 1M25 14h-2.2"/>',
  // The Duck on the Wall: a hand shadow
  duck: '<path d="M4.5 18c0-4.2 3-7 6.8-7 1.8 0 3.1.6 4.1 1.6l6.8-2-3.4 4.4c.6 3.8-2.4 7-7 7H4.5Z"/><circle cx="12.4" cy="14.2" r=".9"/><path d="M3 24.5h22"/><path d="M6 4.5l2 2M14 3v2.6M22 4.5l-2 2" opacity=".6"/>',
  // Saying It Out Loud: a speech balloon, full
  aloud: '<path d="M5.5 5h17a2 2 0 0 1 2 2v9.5a2 2 0 0 1-2 2H13l-5 4.5v-4.5H5.5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z"/><path d="M8.5 10h11M8.5 13.6h7.5"/>',
  // Waiting Well: a chair, kept, and the night outside
  waiting: '<path d="M8.5 4v20.5M8.5 14h11.5v10.5M8.5 18.5H20"/><path d="M22.6 3.4a3.8 3.8 0 1 0 1.6 4.8 3.1 3.1 0 0 1-1.6-4.8Z"/>',
  // Everyone Who Stayed: one figure, gathered round
  stayed: '<circle cx="14" cy="15.5" r="2.5"/><path d="M9.5 25c0-3 2-5 4.5-5s4.5 2 4.5 5"/><circle cx="5.5" cy="12.5" r="1.8"/><circle cx="22.5" cy="12.5" r="1.8"/><circle cx="9.4" cy="6.4" r="1.8"/><circle cx="18.6" cy="6.4" r="1.8"/>',
  // What I Make: a pencil, and the castle it drew
  make: '<path d="M3.5 24.5V14h3v2.5h3V14h3v10.5"/><path d="M3 24.5h13"/><path d="M6.8 24.5v-3.5h2.4v3.5"/><path d="M15.5 17.5 23.2 9.8a1.8 1.8 0 0 1 2.6 2.6L18 20.1l-3.2.6Z"/>'
};

// ---- Units: glyph (28 grid) and portrait; nightmares on the dusk ground -----------------
const UNIT = {
  drifter: '<circle cx="14" cy="6.4" r="2.8"/><path d="M8.5 16.5c0-4 2.4-6.6 5.5-6.6s5.5 2.6 5.5 6.6"/><path d="M8.5 19.6v1.8M19.5 19.6v1.8M11 23.4v1.6M17 23.4v1.6" opacity=".7"/>',
  unflinching: '<circle cx="10.5" cy="7.5" r="2.6"/><path d="M5.5 25c0-6 2-10.2 5-10.2s5 4.2 5 10.2Z"/><path d="M13.8 13 19.4 7.6"/><circle cx="21" cy="6" r="2.2"/><path d="M21 1.6v.8M25.2 6h-.8M24 3l-.6.6"/>',
  laughing: '<circle cx="11" cy="12.5" r="7"/><path d="M7.4 14.2a3.9 3.9 0 0 0 7.2 0Z"/><path d="M7.8 10.4c.6-.8 1.4-.8 2 0M12.2 10.4c.6-.8 1.4-.8 2 0"/><path d="M19.6 7.4c2.8 1.4 4.6 4.2 4.6 7.4a8 8 0 0 1-7.6 8"/>',
  plainspoken: '<circle cx="10" cy="7" r="2.6"/><path d="M5 25c0-6 2-10 5-10s5 4 5 10Z"/><path d="M18 9.5h7M18 13h5M18 16.5h6.2"/>',
  dreamwright: '<path d="M14 2.2l1.2 2.6 2.8.3-2.1 1.9.6 2.8L14 8.4l-2.5 1.4.6-2.8-2.1-1.9 2.8-.3Z"/><path d="M9.5 25.5V17.5a4.5 4.5 0 0 1 9 0v8"/><circle cx="14" cy="13.8" r="1.4"/><path d="M5 25.5h18"/><path d="M4.4 18 9.5 19.6M23.6 18l-5.1 1.6"/>',
  // nightmares
  grinning: '<path d="M4.5 13c2.6 4.8 5.8 7 9.5 7s6.9-2.2 9.5-7"/><path d="M7.4 15l1.2 2.1M10.8 16.8l.6 2.3M14 17.4v2.4M17.2 16.8l-.6 2.3M20.6 15l-1.2 2.1"/><path d="M7.5 8l3.4 1.6M20.5 8l-3.4 1.6"/>',
  voices: '<path d="M5 25V6.5h9V25"/><path d="M3 25h13"/><path d="M17.5 10c1.8 1.8 1.8 4.2 0 6M20.5 7.5c3.2 3.2 3.2 7.8 0 11M23.5 5c4.6 4.6 4.6 11.4 0 16"/>',
  drowned: '<path d="M3 7.5c2.6-2 5.2-2 7.8 0s5.2 2 7.8 0 4.4-1.6 6.4-.6"/><circle cx="14" cy="14.2" r="3.2"/><path d="M8.5 25c0-4.4 2.4-7.4 5.5-7.4s5.5 3 5.5 7.4"/><path d="M12.8 14.2h.1M15.2 14.2h.1"/>',
  wailing: '<path d="M7 25V12a7 7 0 0 1 14 0v13l-2.3-2-2.3 2-2.4-2-2.4 2-2.3-2Z"/><ellipse cx="14" cy="15.6" rx="1.8" ry="2.6"/><path d="M11 10.5h.1M17 10.5h.1"/>',
  stairs: '<path d="M3 25h5v-5h5v-5h5v-5h5V5"/><path d="M19.4 3.4l.9 3.1M22 2.8l.2 3.3M24.6 3.4l-.6 3.1"/><circle cx="7.6" cy="15" r=".8"/><circle cx="10.4" cy="15" r=".8"/>'
};
const NIGHTMARES = ['hollowriders', 'grinning', 'voices', 'drowned', 'wailing', 'stairs'];

// ---- Places (28 grid) and the tone of their reading-band art ---------------------------
const PLACE = {
  thunder:   { tone: 'r', g: '<path d="M8 12.5a4 4 0 0 1 .6-8 5.4 5.4 0 0 1 10.2 1.4 3.4 3.4 0 0 1 .7 6.6Z"/><path d="M14.6 12.5l-2.4 4.2h3.4l-2.2 4.2"/><path d="M2.8 25c4-4 8-5.6 11.2-5.6S21 21 25.2 25"/>' },
  aisle:     { tone: 'w', g: '<path d="M4 25V3.5M24 25V3.5"/><path d="M4 8h5M4 13.5h5M4 19h5M24 8h-5M24 13.5h-5M24 19h-5"/><path d="M9 25V8M19 25V8"/><circle cx="14" cy="17.5" r="1.5"/><path d="M14 19v3.5"/>' },
  deepend:   { tone: 'r', g: '<path d="M8 3.5v14M14 3.5v14"/><path d="M8 7.5h6M8 12h6"/><path d="M3 18.5c2.6-2 5.2-2 7.8 0s5.2 2 7.8 0 4.4-1.6 6.4-.6"/><path d="M3 23.5c2.6-2 5.2-2 7.8 0s5.2 2 7.8 0 4.4-1.6 6.4-.6"/>' },
  principal: { tone: 'w', g: '<path d="M7 25V3.5h14V25"/><path d="M10 7h8v6h-8Z"/><path d="M11 16h6"/><circle cx="18" cy="19.5" r=".9"/><path d="M4 25h20"/>' },
  playground:{ tone: 'g', g: '<path d="M4 25 8 4.5h12L24 25"/><path d="M11 4.5v11M17 4.5v11"/><path d="M10 15.5h8"/><path d="M2.5 25h23"/>' },
  exam:      { tone: 'w', g: '<path d="M6.5 3.5h11L22 8v16.5H6.5Z"/><path d="M17.5 3.5V8H22"/><path d="M9.5 12h9M9.5 15.5h9M9.5 19h4"/><circle cx="18.2" cy="19.8" r="2.2"/>' },
  stage:     { tone: 'd', g: '<path d="M3 3.5h22"/><path d="M3 3.5V25c2.4-3 3.8-8 4.6-21.5M25 3.5V25c-2.4-3-3.8-8-4.6-21.5"/><ellipse cx="14" cy="22" rx="4.6" ry="1.6"/><path d="M14 3.5v3" opacity=".6"/>' },
  diary:     { tone: 'd', g: '<path d="M6.5 4H20v20.5H6.5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Z"/><path d="M4.5 22.5a2 2 0 0 1 2-2H20"/><path d="M17 11h5.5v5H17Z"/><path d="M18.3 11V9.6a1.5 1.5 0 0 1 3 0V11"/>' },
  phone:     { tone: 'r', g: '<path d="M5.5 11c0-3 3.8-5 8.5-5s8.5 2 8.5 5l-3 1.4-1.4-2.6c-2.6-.8-5.6-.8-8.2 0L8.5 12.4Z"/><path d="M9 14.5h10l3 9.5H6Z"/><circle cx="14" cy="19" r="2"/><path d="M3 4.5l2 1.6M25 4.5l-2 1.6"/>' },
  waiting:   { tone: 'g', g: '<path d="M3.5 25v-8.5h5V25M3.5 20.5h5M11.5 25v-8.5h5V25M11.5 20.5h5M19.5 25v-8.5h5V25M19.5 20.5h5"/><path d="M9 4.5h10v6H9Z"/><path d="M12.5 7.5h3"/>' },
  interview: { tone: 'w', g: '<path d="M4 14h20"/><path d="M6 14v10M22 14v10"/><circle cx="9" cy="8" r="2"/><circle cx="14" cy="7" r="2"/><circle cx="19" cy="8" r="2"/><path d="M11.5 25v-4.5h5V25"/>' },
  office:    { tone: 'w', g: '<path d="M7 25V4h14v21"/><path d="M10 8h2M16 8h2M10 12h2M16 12h2M10 16h2M16 16h2"/><path d="M12.5 25v-4.5h3V25"/><path d="M4 25h20"/>' },
  rising:    { tone: 'r', g: '<path d="M6 13 14 6l8 7"/><path d="M8 11.5V16M20 11.5V16"/><path d="M3 18c2.6-2 5.2-2 7.8 0s5.2 2 7.8 0 4.4-1.6 6.4-.6"/><path d="M3 23c2.6-2 5.2-2 7.8 0s5.2 2 7.8 0 4.4-1.6 6.4-.6"/>' },
  chair:     { tone: 'd', g: '<path d="M8 4.5v20.5M8 15h10.5v10M8 19.5h10.5"/><path d="M20.5 12H26M23.2 12v13"/>' },
  faces:     { tone: 'd', g: '<path d="M3 6h6.5v8.5H3ZM10.8 6h6.4v8.5h-6.4ZM18.5 6H25v8.5h-6.5Z"/><circle cx="6.2" cy="9.6" r="1.3"/><circle cx="14" cy="9.6" r="1.3"/><circle cx="21.8" cy="9.6" r="1.3"/><path d="M3 24.5h22"/><path d="M8 24.5v-5M20 24.5v-5"/>' },
  clock:     { tone: 'w', g: '<circle cx="14" cy="13" r="9"/><path d="M14 5.5V7M14 19v1.5M6.5 13H8M20 13h1.5"/><path d="M10.5 22 9 25M17.5 22l1.5 3"/>' },
  mirror:    { tone: 'r', g: '<ellipse cx="14" cy="11" rx="6.5" ry="8.5"/><path d="M14 19.5v5"/><path d="M9 25h10"/><path d="M11.4 6.5 14 10.5l-2 2.5 3 3.5"/>' },
  stairtop:  { tone: 'd', g: '<path d="M3 6h5v5h5v5h5v5h7"/><path d="M3.5 2.5 24.5 18"/><path d="M8 6V3.6M13 11V7.3M18 16v-3.2" opacity=".6"/>' },
  landing:   { tone: 'n', g: '<path d="M3 10.5h22"/><path d="M3 21h22"/><path d="M6.5 10.5V21M10.8 10.5V21M15.2 10.5V21M19.5 10.5V21"/><path d="M3 25h22"/><path d="M3 10.5V6.5M25 10.5V6.5"/>' },
  hallway:   { tone: 'n', g: '<path d="M3 3l7 7M25 3l-7 7M3 25l7-7M25 25l-7-7"/><path d="M10 10h8v8h-8Z"/><path d="M12.5 18v-4.5h3V18"/>' },
  understair:{ tone: 'n', g: '<path d="M3 25h4v-4h4v-4h4v-4h4V9h4V5h2"/><path d="M25 5v20H3"/><path d="M17.5 25v-5.5h4V25"/><circle cx="20.5" cy="22.2" r=".6"/>' },
  wardrobe:  { tone: 'n', g: '<path d="M5 3.5h8.8v19H5Z"/><path d="M14.2 3.5l8.8 2.2v19l-8.8-2.2"/><path d="M12 12v2.2M16.2 12.4v2.2"/><path d="M6.2 22.5V25M21.8 24.7v.3"/>' },
  underbed:  { tone: 'n', g: '<path d="M3 13h22v5H3Z"/><path d="M3 13V8h7v5"/><path d="M3 18v6M25 18v6"/><circle cx="11.5" cy="21.2" r=".9"/><circle cx="15" cy="21.2" r=".9"/>' },
  nobody:    { tone: 'n', g: '<path d="M6 25V3.5h13V25"/><path d="M6 3.5 11 6v21.5L6 25"/><path d="M3 25h22"/><circle cx="14.4" cy="12.5" r=".9"/><circle cx="16.8" cy="12.5" r=".9"/>' }
};

module.exports = { FOCUS, RES, CONSTRUCT, VISION, UNIT, NIGHTMARES, PLACE };
