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
//
// 発注書 (JSON): { "defaults": {...}, "items": [ { "id", "out", "description", "size":[w,h], "direction", "view", "outline", "shading", "detail",
//   "negative", "no_background", "seed", "engine": "pixflux"|"bitforge", "style": "<png path>" (bitforge の style_image), "style_strength": 0-100 } ] }
// 生成物: out の PNG と、同じ場所の <id>.pixellab.json (プロンプト・seed・料金の記録)。
import fs from 'node:fs'
import path from 'node:path'
import os from 'node:os'

const API = 'https://api.pixellab.ai/v1'

function token() {
  if (process.env.PIXELLAB_TOKEN) return process.env.PIXELLAB_TOKEN.trim()
  const f = path.join(os.homedir(), '.config', 'pixellab', 'token')
  if (fs.existsSync(f)) return fs.readFileSync(f, 'utf-8').trim()
  console.error('PIXELLAB_TOKEN が無い。~/.bashrc に export PIXELLAB_TOKEN=... を書くか、~/.config/pixellab/token に鍵だけを保存する (chmod 600)')
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
  return { type: 'base64', base64: 'data:image/png;base64,' + fs.readFileSync(file).toString('base64') }
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
  else { console.error('usage: pixellab.mjs balance | gen <manifest.json> [--only a,b] [--force] [--dry] [--seed N] | rotate <in> <out> [--from d] [--to d] [--size N]'); process.exit(1) }
} catch (e) {
  console.error(String(e.message ?? e))
  process.exit(1)
}
