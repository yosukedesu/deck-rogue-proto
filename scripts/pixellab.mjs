#!/usr/bin/env node
// scripts/pixellab.mjs — PixelLab の REST API でドット絵を作り、Unity の置き場へ保存する (2026-09-07)。
//
// 鍵は環境変数 PIXELLAB_TOKEN か ~/.config/pixellab/token (chmod 600) から読む。チャットや Git には絶対に貼らない。
//
// 使い方:
//   node scripts/pixellab.mjs balance                         # 残高 (USD)
//   node scripts/pixellab.mjs gen docs/pixellab/batch1.json   # 発注書 (manifest) を順に生成
//     --only <id,id>   その id だけ / --force  既にある PNG も作り直す / --dry  送らずに内容だけ表示 / --seed N  seed を上書き
//   node scripts/pixellab.mjs rotate <in.png> <out.png> --from south-east --to south-west [--size 64]
//   node scripts/pixellab.mjs animate <ref.png> <outprefix> --action "..." --description "..." --frames N [--view v] [--direction d] [--seed N]
//
// 発注書 (JSON): { "defaults": {...}, "items": [ { "id", "out", "description", "size":[w,h], "direction", "view", "outline", "shading", "detail",
//   "negative", "no_background", "seed", "engine": "pixflux"|"bitforge", "style": "<png path>" (bitforge の style_image), "style_strength": 0-100 } ] }
// 生成物: out の PNG と、同じ場所の <id>.pixellab.json (プロンプト・seed・料金の記録)。
import fs from 'node:fs'
import path from 'node:path'
import os from 'node:os'

const API = 'https://api.pixellab.ai/v1'

function tokenFiles() {
  const list = []
  if (process.env.PIXELLAB_TOKEN_FILE) list.push(process.env.PIXELLAB_TOKEN_FILE)
  list.push(path.join(os.homedir(), '.config', 'pixellab', 'token'))
  // WSL から Windows 側のホームも見る (PowerShell で保存した場合)。C:\Users\<name>\.config\pixellab\token
  try {
    for (const name of fs.readdirSync('/mnt/c/Users')) {
      const f = path.join('/mnt/c/Users', name, '.config', 'pixellab', 'token')
      if (fs.existsSync(f)) list.push(f)
    }
  } catch (e) { /* WSL でない */ }
  return list
}

function token() {
  if (process.env.PIXELLAB_TOKEN) return process.env.PIXELLAB_TOKEN.trim()
  for (const f of tokenFiles()) {
    if (fs.existsSync(f)) {
      const t = fs.readFileSync(f, 'utf-8').replace(/^\uFEFF/, '').trim()
      if (t) return t
    }
  }
  console.error('PixelLab の鍵が無い。~/.config/pixellab/token (WSL) か C:\\Users\\<name>\\.config\\pixellab\\token (Windows) に鍵だけを保存する')
  process.exit(2)
}

async function call(pathname, body, method = 'POST') {
  const res = await fetch(API + pathname, {
    method,
    headers: { Authorization: 'Bearer ' + token(), 'Content-Type': 'application/json' },
    body: method === 'POST' ? JSON.stringify(body) : undefined,
  })
  const text = await res.text()
  if (!res.ok) throw new Error(`${method} ${pathname} → HTTP ${res.status}: ${text.slice(0, 400)}`)
  return JSON.parse(text)
}

function b64ToPng(b64) {
  const s = b64.startsWith('data:') ? b64.slice(b64.indexOf(',') + 1) : b64
  return Buffer.from(s, 'base64')
}
function pngToB64(file) {
  // 送る側は data: の接頭辞なしの生の base64 (接頭辞つきだと HTTP 500 "Invalid base64-encoded string")
  return { type: 'base64', base64: fs.readFileSync(file).toString('base64') }
}

function args() {
  const a = process.argv.slice(2)
  const opts = {}
  const pos = []
  for (let i = 0; i < a.length; i++) {
    if (a[i].startsWith('--')) {
      const k = a[i].slice(2)
      const next = a[i + 1]
      if (next !== undefined && !next.startsWith('--')) { opts[k] = next; i++ } else opts[k] = true
    } else pos.push(a[i])
  }
  return { pos, opts }
}

async function balance() {
  const r = await call('/balance', null, 'GET')
  console.log(`残高: ${r.usd} ${r.type ?? 'usd'}`)
}

async function gen(manifestPath, opts) {
  const m = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'))
  const defaults = m.defaults ?? {}
  const only = opts.only ? String(opts.only).split(',') : null
  let spent = 0
  for (const item of m.items) {
    if (only && !only.includes(item.id)) continue
    const it = { ...defaults, ...item }
    if (m.style && typeof it.description === 'string') it.description = it.description.replace('{style}', m.style)
    const out = path.resolve(it.out)
    if (fs.existsSync(out) && !opts.force) { console.log(`skip (既にある): ${it.id} → ${it.out}`); continue }
    const [w, h] = it.size
    const engine = it.engine ?? (it.style ? 'bitforge' : 'pixflux')
    const body = {
      description: it.description,
      negative_description: it.negative ?? '',
      image_size: { width: w, height: h },
      text_guidance_scale: it.guidance ?? 8,
      no_background: it.no_background ?? true,
    }
    for (const k of ['outline', 'shading', 'detail', 'view', 'direction', 'isometric']) if (it[k] !== undefined) body[k] = it[k]
    if (opts.seed !== undefined) body.seed = Number(opts.seed)
    else if (it.seed !== undefined) body.seed = it.seed
    if (engine === 'bitforge') {
      if (it.style) body.style_image = pngToB64(path.resolve(it.style))
      if (it.style_strength !== undefined) body.style_strength = it.style_strength
    }
    if (it.init) { body.init_image = pngToB64(path.resolve(it.init)); if (it.init_strength) body.init_image_strength = it.init_strength }
    console.log(`${engine} ${it.id} ${w}×${h} ${it.direction ?? ''}: ${it.description.slice(0, 90)}…`)
    if (opts.dry) continue
    const r = await call('/generate-image-' + engine, body)
    fs.mkdirSync(path.dirname(out), { recursive: true })
    fs.writeFileSync(out, b64ToPng(r.image.base64))
    const meta = { id: it.id, engine, request: { ...body, style_image: it.style, init_image: it.init }, usage: r.usage, at: new Date().toISOString() }
    if (meta.request.style_image === undefined) delete meta.request.style_image
    if (meta.request.init_image === undefined) delete meta.request.init_image
    fs.writeFileSync(out.replace(/\.png$/, '') + '.pixellab.json', JSON.stringify(meta, null, 2))
    spent += r.usage?.usd ?? 0
    console.log(`  → ${it.out}  (${r.usage?.usd ?? '?'} USD)`)
  }
  console.log(`合計 ${spent.toFixed(4)} USD`)
}

// animate-with-text: 参考画像 (64×64) とテキストから N コマを生成し、<prefix>_<i>.png に保存する (2026-09-09 このはの戦闘アニメ)
async function animate(refFile, prefix, opts) {
  const n = Number(opts.frames ?? 4)
  const body = {
    image_size: { width: 64, height: 64 },
    description: String(opts.description ?? ''),
    action: String(opts.action ?? 'idle'),
    negative_description: String(opts.negative ?? ''),
    reference_image: pngToB64(path.resolve(refFile)),
    n_frames: n,
    view: opts.view ?? 'low top-down',
    direction: opts.direction ?? 'south-east',
    text_guidance_scale: Number(opts.guidance ?? 8),
    image_guidance_scale: Number(opts['image-guidance'] ?? 1.4),
  }
  if (opts.color) body.color_image = pngToB64(path.resolve(opts.color))                       // パレットの固定 (元絵を渡す)
  if (opts.init) {                                                                            // 各コマの初期画像 (元絵を n 枚) = 見た目の固定。strength が高いほど動かない
    body.init_images = Array.from({ length: n }, () => pngToB64(path.resolve(opts.init)))
    body.init_image_strength = Number(opts['init-strength'] ?? 300)
  }
  if (opts.seed !== undefined) body.seed = Number(opts.seed)
  console.log(`animate ${path.basename(refFile)} → ${prefix}_[0..${n - 1}] : ${body.action}`)
  if (opts.dry) return
  const r = await call('/animate-with-text', body)
  const dir = path.dirname(path.resolve(prefix))
  fs.mkdirSync(dir, { recursive: true })
  const frames = r.images ?? []
  frames.forEach((im, i) => fs.writeFileSync(`${prefix}_${i}.png`, b64ToPng(im.base64 ?? im)))
  const meta = { engine: 'animate-with-text', request: { ...body, reference_image: refFile }, usage: r.usage, frames: frames.length, at: new Date().toISOString() }
  fs.writeFileSync(`${prefix}.pixellab.json`, JSON.stringify(meta, null, 2))
  console.log(`  → ${frames.length} frames (${r.usage?.usd ?? '?'} USD)`)
}

// estimate-skeleton: 絵から骨格 (18点) を推定して JSON に保存する (2026-09-09)
async function estimateSkeleton(inFile, outFile) {
  const r = await call('/estimate-skeleton', { image: pngToB64(path.resolve(inFile)) })
  fs.writeFileSync(path.resolve(outFile), JSON.stringify(r.keypoints, null, 2))
  console.log(`→ ${outFile} (${r.keypoints.length} keypoints, ${r.usage?.usd ?? '?'} USD)`)
}

// animate-with-skeleton: 参考画像 + 3コマぶんの骨格 (JSON: [[{x,y,label,z_index}...] ×3]) → 3コマ
async function animateSkeleton(refFile, kpFile, prefix, opts) {
  const frames = JSON.parse(fs.readFileSync(path.resolve(kpFile), 'utf-8'))
  const size = Number(opts.size ?? 64)
  const body = {
    image_size: { width: size, height: size },
    reference_image: pngToB64(path.resolve(refFile)),
    skeleton_keypoints: frames,
    guidance_scale: Number(opts.guidance ?? 4),
    view: opts.view ?? 'low top-down',
    direction: opts.direction ?? 'south-east',
  }
  if (opts.color) body.color_image = pngToB64(path.resolve(opts.color))
  if (opts.init) { body.init_images = frames.map(() => pngToB64(path.resolve(opts.init))); body.init_image_strength = Number(opts['init-strength'] ?? 300) }
  if (opts['init-list']) { body.init_images = String(opts['init-list']).split(',').map(f => pngToB64(path.resolve(f))); body.init_image_strength = Number(opts['init-strength'] ?? 300) }
  if (opts['mask-list']) {
    // 白=描き直す。inpainting_images は init と同じ絵 (直したい所だけ白)
    body.inpainting_images = String(opts['init-list'] ?? '').split(',').map(f => pngToB64(path.resolve(f)))
    body.mask_images = String(opts['mask-list']).split(',').map(f => (f && f !== '-') ? pngToB64(path.resolve(f)) : null)
  }
  if (opts.seed !== undefined) body.seed = Number(opts.seed)
  console.log(`animate-skeleton ${path.basename(refFile)} → ${prefix}_[0..2]`)
  if (opts.dry) return
  const r = await call('/animate-with-skeleton', body)
  fs.mkdirSync(path.dirname(path.resolve(prefix)), { recursive: true })
  const imgs = r.images ?? []
  imgs.forEach((im, i) => fs.writeFileSync(`${prefix}_${i}.png`, b64ToPng(im.base64 ?? im)))
  fs.writeFileSync(`${prefix}.pixellab.json`, JSON.stringify({ engine: 'animate-with-skeleton', request: { ...body, reference_image: refFile, init_images: opts.init, color_image: opts.color }, usage: r.usage, frames: imgs.length, at: new Date().toISOString() }, null, 2))
  console.log(`  → ${imgs.length} frames (${r.usage?.usd ?? '?'} USD)`)
}

// inpaint: 白いマスクの範囲だけ描き直す (2026-09-09 斧を消して体だけの参考画像を作る)
async function inpaint(inFile, maskFile, outFile, opts) {
  const size = Number(opts.size ?? 64)
  const body = {
    description: String(opts.description ?? ''),
    negative_description: String(opts.negative ?? ''),
    image_size: { width: size, height: size },
    inpainting_image: pngToB64(path.resolve(inFile)),
    mask_image: pngToB64(path.resolve(maskFile)),
    text_guidance_scale: Number(opts.guidance ?? 3),
    no_background: true,
    view: opts.view ?? 'low top-down',
    direction: opts.direction ?? 'south-east',
    outline: opts.outline ?? 'selective outline',
    shading: opts.shading ?? 'medium shading',
    detail: opts.detail ?? 'highly detailed',
  }
  if (opts.color) body.color_image = pngToB64(path.resolve(opts.color))
  if (opts.seed !== undefined) body.seed = Number(opts.seed)
  console.log(`inpaint ${path.basename(inFile)} → ${outFile}`)
  if (opts.dry) return
  const r = await call('/inpaint', body)
  fs.mkdirSync(path.dirname(path.resolve(outFile)), { recursive: true })
  fs.writeFileSync(path.resolve(outFile), b64ToPng(r.image.base64))
  console.log(`  → ${outFile} (${r.usage?.usd ?? '?'} USD)`)
}

async function rotate(inFile, outFile, opts) {
  const size = Number(opts.size ?? 64)
  const body = {
    image_size: { width: size, height: size },
    from_view: opts['from-view'] ?? 'side',
    to_view: opts['to-view'] ?? 'side',
    from_direction: opts.from ?? 'south-east',
    to_direction: opts.to ?? 'south-west',
    from_image: pngToB64(path.resolve(inFile)),
  }
  if (opts.seed !== undefined) body.seed = Number(opts.seed)
  const r = await call('/rotate', body)
  fs.mkdirSync(path.dirname(path.resolve(outFile)), { recursive: true })
  fs.writeFileSync(path.resolve(outFile), b64ToPng(r.image.base64))
  console.log(`→ ${outFile} (${r.usage?.usd ?? '?'} USD)`)
}

const { pos, opts } = args()
const cmd = pos[0]
try {
  if (cmd === 'balance') await balance()
  else if (cmd === 'gen') await gen(pos[1], opts)
  else if (cmd === 'rotate') await rotate(pos[1], pos[2], opts)
  else if (cmd === 'animate') await animate(pos[1], pos[2], opts)
  else if (cmd === 'skeleton') await estimateSkeleton(pos[1], pos[2])
  else if (cmd === 'inpaint') await inpaint(pos[1], pos[2], pos[3], opts)
  else if (cmd === 'animskel') await animateSkeleton(pos[1], pos[2], pos[3], opts)
  else { console.error('usage: pixellab.mjs balance | gen <manifest.json> [--only a,b] [--force] [--dry] [--seed N] | rotate <in> <out> [--from d] [--to d] [--size N]'); process.exit(1) }
} catch (e) {
  console.error(String(e.message ?? e))
  process.exit(1)
}
