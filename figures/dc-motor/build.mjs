// Fig: a brushed DC motor on a test bench. One wire loop on a former turns between
// two pole pieces; a two-segment commutator and two leaf-spring brushes reverse the
// loop's current every half turn, so the torque always pushes the same way.
// Run: node build.mjs [--light]
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const HERE = dirname(fileURLToPath(import.meta.url));
const SKILL = join(HERE, "..", "..", ".agents", "skills", "anatomy");
const k = await import(join(SKILL, "kit", "iso-kit.mjs"));
const G = await import(join(SKILL, "kit", "lathe.mjs"));
const { kitScript } = await import(join(SKILL, "scripts", "inline-kit.mjs"));
const theme = process.argv.includes("--light") ? "light" : "dark";

// ---------------------------------------------------------------- model (true numbers)
// A small permanent-magnet motor with a single-loop armature.
const M = {
  R: 2.0, //            armature + brush resistance, ohm
  K: 0.0346, //         loop constant, V·s/rad (peak, at |sin φ| = 1)
  J: 2.4e-5, //         rotor + flywheel inertia, kg·m²
  B: 1.2e-5, //         viscous bearing loss, N·m·s/rad
  C: 2.0e-3, //         dry brush friction, N·m
  BRAKE: 0.05, //       Prony brake torque when pressed, N·m
  VMAX: 12, //          supply range, V
  SLOW: 60, //          the drawing runs at 1/60 of true speed
};

// Run the model to steady state so the caption quotes it, not a guess.
function steady(V, brake) {
  let phi = 1.1, w = 0, sumI = 0, sumW = 0, n = 0;
  const h = 2e-5, T = 1.6;
  for (let t = 0; t < T; t += h) {
    const f = Math.abs(Math.sin(phi));
    const I = (V - M.K * f * w) / M.R;
    const loss = M.B * w + (M.C + M.BRAKE * brake) * Math.tanh(w / 4);
    w = Math.max(0, w + (M.K * f * I - loss) / M.J * h);
    phi += w * h;
    if (t > T - 0.4) { sumI += I; sumW += w; n++; }
  }
  return { rpm: (sumW / n) * 60 / (2 * Math.PI), amps: sumI / n };
}
const RUN = steady(12, 0);
const BRAKED = steady(12, 1);
const round = (rpm) => (Math.round(rpm / 100) * 100).toLocaleString("en-US");
console.log("steady 12 V:", RUN, "braked:", BRAKED);

// ---------------------------------------------------------------- world layout
const WIDTH = 600;
const HEIGHT = 390;
const PLATE = { x: -122, y: -62, w: 254, d: 124, r: 8 };
const PLATE_H = 12;
const FOOT_H = 5;
const AX_Z = 46; //               shaft axis height
const SHAFT_R = 2.2;
const RC = 15; //                 loop radius
const X1 = -22; //                far end of the loop
const X2 = 22; //                 near end of the loop
const RL = 4.2; //                lead radius into the commutator
const COM = [32, 46]; //          commutator x span
const COM_R = 5.5;
const FLY = [-88, -78]; //        flywheel x span
const FLY_R = 22;
const PED_FAR = -54; //           bearing pedestals (centre x)
const PED_NEAR = 60;
const YOKE = { x: -26, w: 52 }; // yoke x span
const SUPPLY = { x: 72, y: -56, w: 46, d: 34, r: 3 };
const SUPPLY_Z = [4, 28];
const DIAL = [86, -40];
const DIAL_R = 8.5;
const DIAL_TOP = SUPPLY_Z[1] + 3;
const KNOB = [108, -40];
const KNOB_R = 4.8;
const KNOB_TOP = SUPPLY_Z[1] + 5;
const POSTS = [80, 94]; //        binding posts on the supply front (x)
const POST_Z = 14;
const BRAKE_Y = -30;

const P = k.fitProjection(
  [
    ...k.boxCorners(PLATE, -PLATE_H - FOOT_H, 0),
    [YOKE.x, -34, 92],
    [YOKE.x + YOKE.w, 18, 92],
    [FLY[0], 0, AX_Z + FLY_R],
    [-81, BRAKE_Y, 80],
  ],
  WIDTH,
  HEIGHT,
  { pad: 26, azimuth: 50 },
);

const at = (point) => k.iso(point, P);
const S = (paths, style) => k.solidSvg(paths, style);
const L = (d, style) => k.lineSvg(d, style);
const D = (points, style) => k.dotsSvg(points, style);
const byDepth = (points) => [...points].sort((a, b) => k.depthOf([a[0], a[1], 0], P) - k.depthOf([b[0], b[1], 0], P));
// The shaft frame: s along +x, angle 0 toward +y, angle π/2 toward +z.
const AXIS = G.frameOf([0, 0, AX_Z], [1, 0, 0], [0, 1, 0], [0, 0, 1]);
const along = (o, a, u, v) => G.frameOf(o, a, u, v);
const shaft = (s0, s1) => S(G.disc(s0, s1, SHAFT_R, AXIS, P, { steps: 28 }), { tone: "mid" });

// ---------------------------------------------------------------- base plate
function plate() {
  const out = [`<path class="iso-halo" d="${k.haloOf(PLATE, -PLATE_H - FOOT_H, PLATE_H, P)}"/>`];
  for (const [x, y] of byDepth(k.corners(PLATE, 20))) {
    out.push(S(k.cylinder(x, y, 7, -PLATE_H - FOOT_H, 1.6, P, 28), { tone: "lo" }));
    out.push(S(k.cylinder(x, y, 5.4, -PLATE_H - FOOT_H + 1.6, FOOT_H - 1.6, P, 28), { tone: "mid" }));
  }
  out.push(S(k.slabOf(PLATE, -PLATE_H, PLATE_H, P, 8, 1.6), { tone: "mid", inner: L(k.sideSeam(PLATE, -5, P, 8), { tone: "faint" }) }));
  out.push(L(k.planOutline(k.insetPlan(PLATE, 5), 0, P), { tone: "lo" }));
  const screws = k.corners(PLATE, 10);
  out.push(L(screws.map(([x, y]) => k.ring(x, y, 2.2, 0, P, 16)).join(""), { tone: "lo" }));
  out.push(D(screws.map(([x, y]) => at([x, y, 0])), { size: 0.5 }));
  // ruler strip along the front edge
  const strip = { x: -104, y: PLATE.y + PLATE.d - 15, w: 150, d: 7, r: 1 };
  out.push(S(k.slabOf(strip, 0, 0.6, P, 3), { tone: "lo", crease: "none" }));
  const ticks = k.topTicks(strip.x + 4, strip.x + strip.w - 4, 4, 5, strip.y + 0.8, 0.6, [2, 3.6], P);
  out.push(L(ticks.minor, { tone: "lo" }), L(ticks.major, { tone: "mid" }));
  out.push(D([at([strip.x + 2, strip.y + 3.5, 0.6]), at([strip.x + strip.w - 2, strip.y + 3.5, 0.6])], { size: 0.45 }));
  // label plate, front right
  const label = { x: 70, y: 30, w: 40, d: 18, r: 1.5 };
  out.push(S(k.slabOf(label, 0, 0.8, P, 4), { tone: "lo" }));
  out.push(L(k.planOutline(k.insetPlan(label, 1.6), 0.8, P, 4), { tone: "faint" }));
  [[26, 34.5], [18, 39], [22, 43.5]].forEach(([w, y]) => out.push(L(k.lineOnTop([label.x + 5, y], [label.x + 5 + w, y], 0.8, P), { tone: "lo", free: true })));
  out.push(D([at([label.x + label.w - 4, label.y + 4, 0.8]), at([label.x + label.w - 4, label.y + label.d - 4, 0.8])], { size: 0.45 }));
  return out.join("");
}

// ---------------------------------------------------------------- bench supply (back right)
function supply() {
  const out = [`<path class="iso-halo" d="${k.haloOf(SUPPLY, 0, SUPPLY_Z[1], P)}"/>`];
  for (const [x, y] of byDepth(k.corners(SUPPLY, 6))) out.push(S(k.cylinder(x, y, 3, 0, SUPPLY_Z[0], P, 20), { tone: "lo" }));
  const [z0, z1] = SUPPLY_Z;
  out.push(S(k.slabOf(SUPPLY, z0, z1 - z0, P, 4, 0.8), { tone: "mid", inner: L(k.sideSeam(SUPPLY, z1 - 4, P, 4), { tone: "faint" }) }));
  // vents on the +x face: closed slots
  const vx = SUPPLY.x + SUPPLY.w;
  const vents = [];
  for (let i = 0; i < 5; i++) {
    const y = SUPPLY.y + 8 + i * 4.4;
    vents.push(k.pathOf([at([vx, y, 9]), at([vx, y + 2, 9]), at([vx, y + 2, 19]), at([vx, y, 19])], true));
  }
  out.push(L(vents.join(""), { tone: "lo" }));
  // front panel inset on the +y face
  const fy = SUPPLY.y + SUPPLY.d;
  out.push(L(k.pathOf([at([SUPPLY.x + 4, fy, 7]), at([SUPPLY.x + SUPPLY.w - 4, fy, 7]), at([SUPPLY.x + SUPPLY.w - 4, fy, 22]), at([SUPPLY.x + 4, fy, 22])], true), { tone: "faint" }));
  out.push(D([[SUPPLY.x + 6, 9], [SUPPLY.x + SUPPLY.w - 6, 9], [SUPPLY.x + 6, 20], [SUPPLY.x + SUPPLY.w - 6, 20]].map(([x, z]) => at([x, fy, z])), { size: 0.45 }));
  // top: groove, screws
  out.push(L(k.planOutline(k.insetPlan(SUPPLY, 3), z1, P, 4), { tone: "faint" }));
  out.push(D(k.corners(SUPPLY, 5).map(([x, y]) => at([x, y, z1])), { size: 0.45 }));
  // ammeter, face up
  const [dx, dy] = DIAL;
  out.push(S(k.cylinder(dx, dy, DIAL_R + 1.2, z1, 1.2, P, 48), { tone: "mid" }));
  out.push(S(k.cylinder(dx, dy, DIAL_R, z1 + 1.2, DIAL_TOP - z1 - 1.2, P, 56, 0.5), { tone: "hi", crease: "lo" }));
  out.push(L(k.ring(dx, dy, DIAL_R - 1.4, DIAL_TOP, P, 56), { tone: "lo" }));
  const at0 = -0.75 * Math.PI;
  const ticks = k.radialTicks(dx, dy, DIAL_R - 1.8, 30, 5, DIAL_TOP, [1, 2.1], P, at0, 1.5 * Math.PI);
  out.push(L(ticks.minor, { tone: "lo" }), L(ticks.major, { tone: "mid" }));
  out.push(L(k.ring(dx, dy, 1.1, DIAL_TOP, P, 16), { tone: "mid" }));
  out.push(k.faceTextSvg(k.topMatrix([dx, dy + 4.2], DIAL_TOP, P, "x"), "A", { size: 2.2, tone: "lo", anchor: "middle" }));
  // voltage knob with its scale
  const [kx, ky] = KNOB;
  const kt = k.radialTicks(kx, ky, KNOB_R + 3.4, 12, 3, z1, [1, 1.9], P, at0, 1.5 * Math.PI);
  out.push(L(k.ring(kx, ky, KNOB_R + 3.4, z1, P, 40), { tone: "faint" }));
  out.push(L(kt.minor, { tone: "lo" }), L(kt.major, { tone: "mid" }));
  out.push(S(k.cylinder(kx, ky, KNOB_R + 1, z1, 1, P, 32), { tone: "lo" }));
  out.push(S(k.cylinder(kx, ky, KNOB_R, z1 + 1, KNOB_TOP - z1 - 1, P, 36), { tone: "mid" }));
  out.push(L(k.knurl(kx, ky, KNOB_R, z1 + 1.4, KNOB_TOP, 30, P), { tone: "lo" }));
  // binding posts on the front face
  POSTS.forEach((x, i) => {
    const F = along([x, fy, POST_Z], [0, 1, 0], [1, 0, 0], [0, 0, 1]);
    out.push(S(G.disc(0, 1, 3.4, F, P), { tone: "lo" }));
    out.push(S(G.disc(1, 3.4, 2.6, F, P), { tone: "mid" }));
    out.push(S(G.disc(3.4, 6, 1.8, F, P), { tone: i === 0 ? "hi" : "mid" }));
    out.push(L(G.ribsOf(F, 1.2, 3.2, 2.6, 6, P, { seams: false }), { tone: "lo" }));
  });
  return out.join("");
}
const needleAt = (amps) => {
  const angle = -0.75 * Math.PI + Math.min(1, Math.max(0, amps / 6)) * 1.5 * Math.PI;
  const [x, y] = DIAL;
  return k.lineOnTop([x - Math.sin(angle) * 1.8, y + Math.cos(angle) * 1.8], [x + Math.sin(angle) * (DIAL_R - 2.4), y - Math.cos(angle) * (DIAL_R - 2.4)], DIAL_TOP + 0.2, P);
};
const pointerAt = (volts) => {
  const angle = -0.75 * Math.PI + (volts / 12) * 1.5 * Math.PI;
  const [x, y] = KNOB;
  return k.lineOnTop([x + Math.sin(angle) * 1.2, y - Math.cos(angle) * 1.2], [x + Math.sin(angle) * (KNOB_R - 0.8), y - Math.cos(angle) * (KNOB_R - 0.8)], KNOB_TOP, P);
};

// ---------------------------------------------------------------- bearing pedestal
function pedestal(x) {
  const out = [];
  const plinth = { x: x - 8, y: -14, w: 16, d: 28, r: 2 };
  out.push(S(k.slabOf(plinth, 0, 5, P, 3, 0.5), { tone: "mid" }));
  out.push(D(byDepth([[x - 4.5, -10.5], [x + 4.5, -10.5], [x - 4.5, 10.5], [x + 4.5, 10.5]]).map(([px, py]) => at([px, py, 5])), { size: 0.5 }));
  out.push(S(k.slabOf({ x: x - 3.5, y: -7, w: 7, d: 14, r: 1 }, 5, AX_Z - 5, P, 3, 0.4), { tone: "mid" }));
  return out.join("");
}
function housing(x) {
  const out = [];
  out.push(S(G.disc(x - 4, x + 4, 7.2, AXIS, P), { tone: "mid", inner: L(G.arcOf(AXIS, x, 7.2, P), { tone: "faint" }) }));
  out.push(L(G.circleOf(AXIS, x + 4, 4.4, P), { tone: "lo" }));
  // oil cup on top
  out.push(S(k.cylinder(x, 0, 1.6, AX_Z + 6.9, 2.4, P, 16), { tone: "mid" }));
  out.push(S(k.cylinder(x, 0, 2.3, AX_Z + 9.3, 1, P, 20), { tone: "mid" }));
  return out.join("");
}

// ---------------------------------------------------------------- flywheel and brake
function brakeBack() {
  const out = [];
  const bx = -83;
  out.push(S(k.slabOf({ x: bx - 6, y: BRAKE_Y - 6, w: 12, d: 12, r: 1.5 }, 0, 4, P, 3, 0.5), { tone: "mid" }));
  out.push(D([at([bx - 3.5, BRAKE_Y - 3.5, 4]), at([bx + 3.5, BRAKE_Y + 3.5, 4])], { size: 0.45 }));
  out.push(S(k.cylinder(bx, BRAKE_Y, 2.6, 4, 70, P, 24), { tone: "mid", crease: "none", inner: L(k.sideArc(bx, BRAKE_Y, 2.6, 40, P, 14), { tone: "faint" }) }));
  out.push(S(k.slabOf({ x: bx - 4.5, y: BRAKE_Y - 4.5, w: 9, d: 9, r: 1.5 }, 72, 7, P, 3, 0.5), { tone: "mid" }));
  return out.join("");
}
function flywheel() {
  const out = [];
  const [f0, f1] = FLY;
  out.push(S(G.disc(f0 - 6, f0, SHAFT_R, AXIS, P, { steps: 24 }), { tone: "mid" }));
  out.push(S(G.disc(f0, f1, FLY_R, AXIS, P), { tone: "mid", inner: L(G.arcOf(AXIS, f0 + 5, FLY_R, P), { tone: "faint" }) }));
  out.push(L(G.circleOf(AXIS, f1, FLY_R - 3, P), { tone: "lo" }));
  out.push(S(G.disc(f1, f1 + 3, 6, AXIS, P), { tone: "mid" }));
  return out.join("");
}
function brakeLever() {
  const out = [];
  const bx = -83;
  const top = AX_Z + FLY_R; // rim top
  out.push(S(k.slabOf({ x: bx - 2, y: BRAKE_Y + 4.5, w: 4, d: 40, r: 1 }, 74, 4, P, 2, 0.4), { tone: "mid" }));
  out.push(S(k.slabOf({ x: bx - 3, y: -5, w: 6, d: 10, r: 1 }, top + 0.6, 74 - top - 0.6, P, 2, 0.3), { tone: "hi", crease: "lo" }));
  out.push(D([at([bx, BRAKE_Y + 8, 78]), at([bx, 9, 78])], { size: 0.45 }));
  // adjusting knob at the lever's free end
  out.push(S(k.cylinder(bx, 12, 3.4, 78, 4, P, 28), { tone: "mid" }));
  out.push(L(k.knurl(bx, 12, 3.4, 78, 82, 22, P), { tone: "lo" }));
  return out.join("");
}

// ---------------------------------------------------------------- magnet yoke
const POLE = { x: -22, y: -14, w: 44, d: 28, r: 2 };
function yokeBack() {
  const out = [];
  const base = { x: YOKE.x, y: -36, w: YOKE.w, d: 54, r: 2.5 };
  out.push(`<path class="iso-halo" d="${k.haloOf(base, 0, 8, P)}"/>`);
  out.push(S(k.slabOf(base, 0, 8, P, 4, 0.8), { tone: "mid" }));
  out.push(D([[base.x + 5, 13], [base.x + base.w - 5, 13]].map(([x, y]) => at([x, y, 8])), { size: 0.5 }));
  out.push(S(k.slabOf({ x: YOKE.x, y: -36, w: YOKE.w, d: 10, r: 2 }, 8, 76, P, 4, 0.6), { tone: "mid" }));
  out.push(S(k.slabOf(POLE, 8, 18, P, 4, 0.6), { tone: "lo", inner: L(k.sideSeam(POLE, 20, P, 4), { tone: "faint" }) }));
  // field lines in the back plane, pole face to pole face
  const lines = [];
  for (let x = -16; x <= 16; x += 8) lines.push(k.segment([x, -13.7, 26], [x, -13.7, 66], P));
  out.push(L(lines.join(""), { tone: "lo", dotted: true }));
  return out.join("");
}
function yokeFront() {
  const out = [];
  out.push(S(k.slabOf(POLE, 66, 18, P, 4, 0.6), { tone: "lo", inner: L(k.sideSeam(POLE, 70, P, 4), { tone: "faint" }) }));
  const arm = { x: YOKE.x, y: -36, w: YOKE.w, d: 54, r: 2.5 };
  out.push(S(k.slabOf(arm, 84, 8, P, 4, 0.8), { tone: "mid", inner: L(k.sideSeam(arm, 88, P, 4), { tone: "faint" }) }));
  out.push(D(byDepth(k.corners(arm, 4.5)).map(([x, y]) => at([x, y, 92])), { size: 0.5 }));
  out.push(L(k.planOutline({ x: -14, y: -8, w: 28, d: 16, r: 1.5 }, 92, P, 3), { tone: "faint" }));
  out.push(k.faceTextSvg(k.topMatrix([0, 1.4], 92, P, "x"), "N", { size: 7, tone: "lo", anchor: "middle" }));
  return out.join("");
}

// ---------------------------------------------------------------- brushes and terminals
const BRUSH = [37, 41];
function block(side) {
  const y0 = side > 0 ? 18 : -25;
  const plan = { x: 33, y: y0, w: 13, d: 7, r: 1 };
  const out = [S(k.slabOf(plan, 0, 7, P, 3, 0.5), { tone: "lo" })];
  const yc = y0 + 3.5;
  out.push(S(k.slabOf({ x: 36, y: yc - 2, w: 6, d: 4, r: 0.6 }, 7, 1.4, P, 2), { tone: "mid" }));
  out.push(D([at([39, yc - 1.2, 8.4])], { size: 0.45 }));
  out.push(S(k.cylinder(44, yc, 1.4, 7, 1.2, P, 16), { tone: "mid" }));
  out.push(L(k.lineOnTop([42.8, yc], [45.2, yc], 8.2, P), { tone: "lo" }));
  return out.join("");
}
function brush(side) {
  // A brass leaf spring from the clamp up to the commutator side.
  const foot = [side * 21.5, 8.4 - AX_Z];
  const tip = [side * (COM_R + 0.05), 0];
  const dir = [tip[0] - foot[0], tip[1] - foot[1]];
  const n = Math.hypot(dir[0], dir[1]);
  const out = [side * (dir[1] / n) * 0.9, -side * (dir[0] / n) * 0.9];
  const outward = out[0] * side > 0 ? out : [-out[0], -out[1]];
  const poly = [foot, tip, [tip[0] + outward[0], tip[1] + outward[1]], [foot[0] + outward[0], foot[1] + outward[1]]];
  return S(G.prismOf(AXIS, poly, BRUSH[0], BRUSH[1], P), { tone: "hi", crease: "lo" });
}
function wires() {
  const route = (from, legs, to) => k.wireSvg(at(from), at(to), { via: legs.map(at), tone: "mid", size: 1 });
  const fy = SUPPLY.y + SUPPLY.d + 6;
  return [
    route([POSTS[0], fy, POST_Z], [[POSTS[0], fy + 1.5, 0], [49, fy + 1.5, 0], [49, 21.5, 0], [46.8, 21.5, 0], [46.8, 21.5, 7.2]], [44, 21.5, 8.3]),
    route([POSTS[1], fy, POST_Z], [[POSTS[1], fy - 0.5, 0], [47.5, fy - 0.5, 0], [47.5, -21.5, 0], [46.8, -21.5, 7.2]], [44, -21.5, 8.3]),
  ].join("");
}

// ---------------------------------------------------------------- commutator (static body)
function commutator() {
  const out = [];
  out.push(S(G.disc(COM[0] - 1.6, COM[0], COM_R + 0.8, AXIS, P), { tone: "lo" }));
  out.push(S(G.disc(COM[0], COM[1], COM_R, AXIS, P), { tone: "hi", crease: "lo" }));
  out.push(L(G.circleOf(AXIS, COM[1], SHAFT_R + 0.9, P), { tone: "lo" }));
  return out.join("");
}

// ---------------------------------------------------------------- page
const ROTOR_IDS = ["coil-back", "former", "coil-front"];
const svg = k.figureSvg({
  width: WIDTH,
  height: HEIGHT,
  label:
    "A brushed DC motor on a test bench. A single wire loop wound on a former turns on a shaft between a north pole piece above and a south pole piece below. At the near end a split-ring commutator of two copper half rings is pressed by two brass leaf brushes wired to a bench supply with a voltage knob and an ammeter. At the far end a flywheel carries a brake shoe on a lever.",
  body: [
    plate(),
    supply(),
    `<path data-part="needle" class="iso-line" data-tone="hi" data-free="start" d="${needleAt(0)}" style="stroke-width:1"/>`,
    `<path data-part="pointer" class="iso-line" data-tone="hi" data-free="both" d="${pointerAt(0)}" style="stroke-width:1"/>`,
    brakeBack(),
    flywheel(),
    `<g data-part="fly-marks"></g>`,
    `<g data-part="brake">${brakeLever()}</g>`,
    shaft(FLY[1] + 3, PED_FAR - 4),
    pedestal(PED_FAR),
    housing(PED_FAR),
    yokeBack(),
    shaft(PED_FAR + 4, X1 - 1),
    ...ROTOR_IDS.map((id) => `<g data-part="${id}"></g>`),
    yokeFront(),
    block(-1),
    brush(-1),
    block(1),
    wires(),
    shaft(X2 - 1, COM[0] - 1.6),
    commutator(),
    `<g data-part="com-gaps"></g>`,
    brush(1),
    pedestal(PED_NEAR),
    shaft(COM[1], PED_NEAR - 4),
    housing(PED_NEAR),
    shaft(PED_NEAR + 4, PED_NEAR + 11),
    S(G.disc(PED_NEAR + 5, PED_NEAR + 8, 3.6, AXIS, P), { tone: "mid" }),
  ],
});

const latheModule = readFileSync(join(SKILL, "kit", "lathe.mjs"), "utf8").replace(/^import .*$/gm, "");
const latheNames = [...latheModule.matchAll(/^export (?:const|function) ([A-Za-z_0-9]+)/gm)].map((m) => m[1]);
const latheScript = `const G = (() => {\n${latheModule.replace(/^export /gm, "")}\nreturn { ${latheNames.join(", ")} };\n})();\n`;

const live = `
const P = ${JSON.stringify(P)};
const M = ${JSON.stringify(M)};
const AX_Z = ${AX_Z}, RC = ${RC}, X1 = ${X1}, X2 = ${X2}, RL = ${RL}, COM = ${JSON.stringify(COM)}, COM_R = ${COM_R}, SHAFT_R = ${SHAFT_R};
const FLY = ${JSON.stringify(FLY)}, FLY_R = ${FLY_R};
const DIAL = ${JSON.stringify(DIAL)}, DIAL_R = ${DIAL_R}, DIAL_TOP = ${DIAL_TOP}, KNOB = ${JSON.stringify(KNOB)}, KNOB_R = ${KNOB_R}, KNOB_TOP = ${KNOB_TOP};
const AXIS = G.frameOf([0, 0, AX_Z], [1, 0, 0], [0, 1, 0], [0, 0, 1]);
const V3 = G.viewOf(P);
const stage = document.querySelector(".iso-stage");
const svg = stage.querySelector("svg");
const part = (name) => svg.querySelector('[data-part="' + name + '"]');
const coilBack = part("coil-back"), former = part("former"), coilFront = part("coil-front");
const gaps = part("com-gaps"), marks = part("fly-marks"), brake = part("brake"), needle = part("needle"), pointer = part("pointer");
const readout = document.querySelector("[data-readout]");
const still = matchMedia("(prefers-reduced-motion: reduce)");
const needleAt = ${needleAt.toString().replace(/k\./g, "")};
const pointerAt = ${pointerAt.toString().replace(/k\./g, "")};
const pt = (x, r, a) => [x, r * Math.cos(a), AX_Z + r * Math.sin(a)];
const sc = G.scaleOf(P);
const f1 = (v) => v.toFixed(1);

// ---- the model: one loop, a two-segment commutator, true numbers
const state = { phi: 1.1, w: 0, I: 0, Iavg: 0, eAvg: 0, V: 0, Vset: 0, brake: 0, brakeSet: 0, flow: 0 };
function step(h) {
  const f = Math.abs(Math.sin(state.phi));
  const e = M.K * f * state.w;
  const I = state.V < 0.05 ? 0 : (state.V - e) / M.R;
  const drive = M.K * f * I;
  const loss = M.B * state.w + (M.C + M.BRAKE * state.brake) * Math.tanh(state.w / 4);
  state.w = Math.max(0, state.w + (drive - loss) / M.J * h);
  state.phi = (state.phi + state.w * h) % (Math.PI * 2);
  state.I = I;
  const a = Math.min(1, h / 0.03);
  state.Iavg += (I - state.Iavg) * a;
  state.eAvg += (e - state.eAvg) * a;
}

// ---- drawing the parts that turn
function tube(points, r, cls) {
  const d = points.map((p, i) => (i ? "L" : "M") + at3(p)).join("");
  const w = (2 * r * sc).toFixed(2);
  return '<path class="dm-wire-edge" style="stroke-width:' + (2 * r * sc + 1.1).toFixed(2) + 'px" d="' + d + '"/>' +
    '<path class="dm-wire ' + (cls || "") + '" style="stroke-width:' + w + 'px" d="' + d + '"/>' +
    '<path class="dm-wire-shine" style="stroke-width:' + (0.5 * r * sc).toFixed(2) + 'px" d="' + d + '"/>';
}
const at3 = (p) => { const s = iso(p, P); return s[0].toFixed(1) + " " + s[1].toFixed(1); };
const depth = (p) => depthOf(p, P);
function loopPath(phi) {
  // current path: commutator lug A -> out -> along side A -> across far end -> along side B -> in -> lug B
  const a = phi, b = phi + Math.PI;
  const arc = [];
  for (let i = 0; i <= 10; i++) arc.push(pt(X1, 4.6, a + (i / 10) * Math.PI));
  return {
    leadA: [pt(COM[0], RL, a), pt(X2, RL, a), pt(X2, RC, a)],
    sideA: [pt(X2, RC, a), pt(X1, RC, a)],
    farEnd: [pt(X1, RC, a), ...arc, pt(X1, RC, b)],
    sideB: [pt(X1, RC, b), pt(X2, RC, b)],
    leadB: [pt(X2, RC, b), pt(X2, RL, b), pt(COM[0], RL, b)],
  };
}
function drawRotor() {
  const phi = state.phi;
  const L = loopPath(phi);
  const axisDepth = depth([0, 0, AX_Z]);
  const pieces = [
    ["leadA", L.leadA, 0.75], ["sideA", L.sideA, 1.6], ["farEnd", L.farEnd, 1.6], ["sideB", L.sideB, 1.6], ["leadB", L.leadB, 0.75],
  ].map(([name, pts, r]) => ({ name, pts, r, z: pts.reduce((s, p) => s + depth(p), 0) / pts.length }));
  pieces.sort((p, q) => p.z - q.z);
  // charge carriers: dots along the path, moving with the current
  const path = [...L.leadA, ...L.sideA.slice(1), ...L.farEnd.slice(1), ...L.sideB.slice(1), ...L.leadB.slice(1)];
  const seg = [];
  let total = 0;
  for (let i = 1; i < path.length; i++) { const l = G.len3(G.sub3(path[i], path[i - 1])); seg.push(l); total += l; }
  const dots = [];
  const gap = 7;
  const lit = Math.abs(state.I) > 0.02 && state.V >= 0.05;
  if (lit) {
    for (let s = ((state.flow % gap) + gap) % gap; s < total; s += gap) {
      let rem = s, i = 0;
      while (i < seg.length - 1 && rem > seg[i]) { rem -= seg[i]; i++; }
      const t = Math.min(1, rem / seg[i]);
      const p = G.lerp3(path[i], path[i + 1], t);
      dots.push(p);
    }
  }
  const back = [], front = [];
  for (const piece of pieces) (piece.z < axisDepth ? back : front).push(tube(piece.pts, piece.r, piece.name.startsWith("lead") ? "dm-lead" : ""));
  const dotSvg = (list) => list.length ? '<g class="dm-carriers">' + list.map((p) => { const s = iso(p, P); return '<circle cx="' + s[0].toFixed(1) + '" cy="' + s[1].toFixed(1) + '" r="0.75"/>'; }).join("") + "</g>" : "";
  back.push(dotSvg(dots.filter((p) => depth(p) < axisDepth)));
  front.push(dotSvg(dots.filter((p) => depth(p) >= axisDepth)));
  coilBack.innerHTML = back.join("");
  coilFront.innerHTML = front.join("");
  // the former: a thin board in the loop's plane, through the shaft
  const c = Math.cos(phi), s = Math.sin(phi), hw = RC - 1.8, ht = 0.9;
  const poly = [[c * hw - s * ht, s * hw + c * ht], [-c * hw - s * ht, -s * hw + c * ht], [-c * hw + s * ht, -s * hw - c * ht], [c * hw + s * ht, s * hw - c * ht]];
  former.innerHTML = solidSvg(G.prismOf(AXIS, poly, X1 + 1.4, X2 - 1.4, P), { tone: "mid" }) +
    solidSvg(G.disc(X1 - 1, X2 + 1, SHAFT_R, AXIS, P, { steps: 24 }), { tone: "mid" });
  // commutator gaps lie in the loop's plane, at the angles of the two sides
  const g = [];
  for (const a of [phi, phi + Math.PI]) {
    const n = [0, Math.cos(a), Math.sin(a)];
    if (G.dot3(n, V3) > 0.06) g.push(pathOf([iso(pt(COM[0], COM_R, a), P), iso(pt(COM[1], COM_R, a), P)]));
    g.push(pathOf([iso(pt(COM[1], SHAFT_R + 0.9, a), P), iso(pt(COM[1], COM_R, a), P)]));
  }
  gaps.innerHTML = '<path class="dm-gap" d="' + g.join("") + '"/>';
  // flywheel: reflective strip and bolt circle turn with the shaft
  const stripe = [pt(FLY[1], 7, phi - 0.09), pt(FLY[1], FLY_R - 3.6, phi - 0.05), pt(FLY[1], FLY_R - 3.6, phi + 0.05), pt(FLY[1], 7, phi + 0.09)];
  const bolts = [];
  for (let i = 0; i < 6; i++) bolts.push(iso(pt(FLY[1], 12.5, phi + Math.PI / 6 + (i * Math.PI) / 3), P));
  marks.innerHTML = '<path class="dm-stripe" d="' + pathOf(stripe.map((p) => iso(p, P)), true) + '"/>' + dotsSvg(bolts, { size: 0.6, tone: "mid" });
}
function draw() {
  drawRotor();
  needle.setAttribute("d", needleAt(state.Iavg));
  pointer.setAttribute("d", pointerAt(state.V));
  brake.setAttribute("transform", translateAlong(P, [0, 0, -0.6 * state.brake]));
  const rpm = state.w * 60 / (2 * Math.PI);
  const text = f1(state.V) + " V · " + Math.max(0, state.Iavg).toFixed(2) + " A · " + Math.round(rpm).toLocaleString("en-US") + " rpm" + (state.brake > 0.5 ? " · braked" : "");
  if (readout.textContent !== text) readout.textContent = text;
  stage.setAttribute("aria-valuenow", f1(state.V));
  stage.setAttribute("aria-valuetext", text + ", back EMF " + f1(state.eAvg) + " V");
}

// ---- motion
const TOUR = [[0, 0, 2], [6, 0, 6], [12, 0, 7], [12, 1, 6], [12, 0, 5], [0, 0, 7]];
const TOUR_T = TOUR.reduce((s, x) => s + x[2], 0);
const tourAt = (t) => { t %= TOUR_T; for (const s of TOUR) { if (t < s[2]) return s; t -= s[2]; } return TOUR[0]; };
let frame = 0, last = 0, clock = 0, touring = true, visible = false, idle = 0;
function tick(now) {
  const dt = last ? Math.min((now - last) / 1000, 1 / 30) : 1 / 60;
  last = now;
  if (touring) { clock += dt; const s = tourAt(clock); state.Vset = s[0]; state.brakeSet = s[1]; }
  state.V += (state.Vset - state.V) * (1 - Math.exp(-dt / 0.25));
  if (Math.abs(state.Vset - state.V) < 0.005) state.V = state.Vset;
  state.brake += (state.brakeSet - state.brake) * (1 - Math.exp(-dt / 0.12));
  const h = dt / M.SLOW, n = 24;
  for (let i = 0; i < n; i++) step(h / n);
  state.flow += Math.max(0, state.Iavg) * dt * 9;
  draw();
  const settled = !touring && state.V === state.Vset && state.w === 0 && state.Vset === 0;
  frame = visible && !settled ? requestAnimationFrame(tick) : 0;
}
const run = () => { if (!frame && visible && !still.matches) { last = 0; frame = requestAnimationFrame(tick); } };
const steer = (V, b) => {
  touring = false;
  if (V !== null) state.Vset = Math.min(M.VMAX, Math.max(0, V));
  if (b !== null) state.brakeSet = b;
  clearTimeout(idle);
  idle = setTimeout(() => { touring = !still.matches; clock = 0; run(); }, 12000);
  run();
};
function settleStill() {
  // reduced motion: one frame at 12 V, after the model has reached steady running
  state.V = state.Vset = 12;
  for (let i = 0; i < 40000; i++) step(5e-5);
  state.phi = 1.1;
  draw();
}
new IntersectionObserver((entries) => { visible = entries[entries.length - 1].isIntersecting; if (visible) run(); }, { rootMargin: "120px 0px" }).observe(stage);
stage.addEventListener("pointermove", (event) => {
  const box = svg.getBoundingClientRect();
  const share = Math.min(1, Math.max(0, ((event.clientX - box.left) / box.width - 0.15) / 0.7));
  steer(Math.round(share * 24) / 2, null);
});
stage.addEventListener("pointerdown", () => steer(null, 1));
stage.addEventListener("pointerup", () => steer(null, 0));
stage.addEventListener("pointerleave", () => steer(null, 0));
stage.addEventListener("keydown", (event) => {
  if (event.key === "ArrowRight" || event.key === "ArrowUp") steer(state.Vset + 1, null);
  else if (event.key === "ArrowLeft" || event.key === "ArrowDown") steer(state.Vset - 1, null);
  else if (event.key === "Home") steer(0, null);
  else if (event.key === "End") steer(M.VMAX, null);
  else if (event.key === " ") steer(null, state.brakeSet ? 0 : 1);
  else return;
  event.preventDefault();
});
if (still.matches) { touring = false; settleStill(); } else draw();
`;

const css = `
.dm-wire-edge{fill:none;stroke:var(--anatomy-lit);stroke-linecap:round;stroke-linejoin:round;vector-effect:none!important}
.dm-wire{fill:none;stroke:var(--anatomy-lit-shade);stroke-linecap:round;stroke-linejoin:round;vector-effect:none!important}
.dm-wire-shine{fill:none;stroke:var(--anatomy-lit-top);stroke-linecap:round;stroke-linejoin:round;vector-effect:none!important;transform:translate(-0.25px,-0.35px)}
.dm-carriers circle{fill:var(--anatomy-lit);stroke:var(--anatomy-paper);stroke-width:.5}
.dm-gap{fill:none;stroke:var(--anatomy-hi);stroke-width:1px;stroke-linecap:butt}
.dm-stripe{fill:var(--anatomy-top);stroke:var(--anatomy-hi);stroke-width:var(--anatomy-weight);stroke-linejoin:round}
.iso-stage{touch-action:pan-y;outline:none;cursor:ew-resize}
.iso-stage:focus-visible{box-shadow:0 0 0 1px var(--anatomy-mid)}
`;

const body = `<style>${css}</style>` + k.plateHtml({
  fig: "Fig 1",
  title: "DC motor, running",
  hint: "Move across for volts · press to brake",
  readout: "0.0 V · 0.00 A · 0 rpm",
  keys: [
    { mark: "lit", label: "Armature loop: dots move with the current" },
    { mark: "raised", label: "Split-ring commutator and brass brushes" },
    { mark: "dotted", label: "Field, N pole above to S pole below" },
  ],
  caption:
    `A single-loop DC motor. Current through the loop sits across the magnet's field, so each side is pushed sideways and the loop turns. Every half turn the gaps of the commutator pass the brushes and the current in the loop reverses, so the push keeps the same sense. As the loop speeds up it generates a back EMF, k·ω, that eats into the supply voltage until only enough current flows to cover friction: at 12 V it settles near ${round(RUN.rpm)} rpm on ${RUN.amps.toFixed(1)} A, and with the brake on it slows to ${round(BRAKED.rpm)} rpm while the current climbs to ${BRAKED.amps.toFixed(1)} A. Numbers are from the model (R = 2 Ω, k = 0.035 V·s/rad); the drawing turns at 1/60 of true speed.`,
  body: `<div class="iso-stage" tabindex="0" role="slider" aria-label="DC motor: left and right arrows set the supply voltage, space presses the brake" aria-valuemin="0" aria-valuemax="12" aria-valuenow="0">${svg}</div>`,
});

const file = theme === "light" ? "dc-motor-light.html" : "dc-motor.html";
writeFileSync(join(HERE, file), k.pageHtml({ title: "DC motor", theme, body, script: kitScript() + latheScript + live }));
console.log("wrote", file);
