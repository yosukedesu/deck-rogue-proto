#!/usr/bin/env node
// scripts/pixellab-v2.mjs — PixelLab の v2 API (Pro モデル) を呼ぶ (2026-09-29 敵の絵の地面を消し、向きをそろえる)。
//
// 鍵は scripts/pixellab.mjs と同じ置き場 (PIXELLAB_TOKEN / ~/.config/pixellab/token / Windows 側のホーム)。
//
// 使い方:
//   node scripts/pixellab-v2.mjs rot8 <in.png> <outdir> [--id X] [--size N] [--view "low top-down"] [--seed N]
//        今の絵を参照にして8方向を作る (generate-8-rotations-v2 / rotate_character / 背景なし)。
//        <outdir>/<id>__<方向>.png を8枚。方向は画面で見た向き (敵に使うのは south-west＝斜め左手前＝プレイヤーの方)。
//        減色版 (元の絵の色数に合わせた quantized_images) は <id>__<方向>__q.png
//   node scripts/pixellab-v2.mjs edit <in.png> <out.png> --text "..." [--size N] [--seed N] [--n K]
//        文で直す (edit-images-v2 / edit_with_text / 背景なし)。--n で同じ絵を K 枚まで並べて候補を増やす
import fs from 'node:fs'
import path from 'node:path'
import os from 'node:os'

const API = 'https://api.pixellab.ai/v2'
// 返りの8枚の並び = 画面で見た向き: 正面 → 斜め右手前 → 右 → 斜め右奥 → 背中 → 斜め左奥 → 左 → 斜め左手前。
// (API の説明は South, South-West, West… だが、それはキャラ自身から見た向き。2026-09-29 に取り違えて敵が正面・右を向いた)
const DIRS = ['south', 'south-east', 'east', 'north-east', 'north', 'north-west', 'west', 'south-west']

function token() {
  if (process.env.PIXELLAB_TOKEN) return process.env.PIXELLAB_TOKEN.trim()
  const list = []
  if (process.env.PIXELLAB_TOKEN_FILE) list.push(process.env.PIXELLAB_TOKEN_FILE)
  list.push(path.join(os.homedir(), '.config', 'pixellab', 'token'))
  try {
    for (const name of fs.readdirSync('/mnt/c/Users')) list.push(path.join('/mnt/c/Users', name, '.config', 'pixellab', 'token'))
  } catch { /* WSL でない */ }
  for (const f of list) {
    if (fs.existsSync(f)) {
      const t = fs.readFileSync(f, 'utf-8').replace(/^﻿/, '').trim()
      if (t) return t
    }
  }
  console.error('PixelLab の鍵が無い (scripts/pixellab.mjs と同じ置き場に置く)')
  process.exit(2)
}

async function call(pathname, body, method = 'POST') {
  for (let attempt = 1; ; attempt++) {
    const res = await fetch((pathname.startsWith('http') ? pathname : API + pathname), {   // http で始まれば v1 など別の置き場 (2026-09-30 order の pixflux)
      method,
      headers: { Authorization: 'Bearer ' + token(), 'Content-Type': 'application/json' },
      body: method === 'POST' ? JSON.stringify(body) : undefined,
    })
    const text = await res.text()
    if (res.ok) return JSON.parse(text)
    if ((res.status >= 500 || res.status === 429) && attempt < 5) {
      await new Promise((r) => setTimeout(r, attempt * 10000))
      continue
    }
    throw new Error(`${method} ${pathname} → HTTP ${res.status}: ${text.slice(0, 600)}`)
  }
}

/** 背景ジョブを完了まで待つ (5秒おき・最大10分) */
async function waitJob(id) {
  for (let i = 0; i < 120; i++) {
    const r = await call(`/background-jobs/${id}`, null, 'GET')
    if (r.status === 'completed') return r
    if (r.status === 'failed') throw new Error(`job ${id} failed: ${JSON.stringify(r).slice(0, 600)}`)
    await new Promise((res) => setTimeout(res, 5000))
  }
  throw new Error(`job ${id} timeout`)
}

/** 返りの JSON から base64 の画像を出てきた順に拾う (形が endpoint ごとに違うので再帰で探す) */
function collectImages(node, out = []) {
  if (node === null || node === undefined) return out
  if (Array.isArray(node)) { for (const x of node) collectImages(x, out); return out }
  if (typeof node === 'object') {
    if (typeof node.base64 === 'string') { out.push(node.base64); return out }
    for (const k of Object.keys(node)) collectImages(node[k], out)
  }
  return out
}

function b64ToPng(b64) {
  const s = b64.startsWith('data:') ? b64.slice(b64.indexOf(',') + 1) : b64
  return Buffer.from(s, 'base64')
}
function pngSize(file) {
  const b = fs.readFileSync(file)
  return { width: b.readUInt32BE(16), height: b.readUInt32BE(20) }
}
function imgRef(file) {
  const { width, height } = pngSize(file)
  return { image: { type: 'base64', base64: fs.readFileSync(file).toString('base64'), format: 'png' }, width, height }
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

async function rot8(inFile, outDir, opts) {
  const ref = imgRef(inFile)
  const size = Number(opts.size ?? ref.width)
  const id = opts.id ?? path.basename(inFile, '.png')
  const body = {
    method: 'rotate_character',
    reference_image: ref,
    image_size: { width: size, height: size },
    view: opts.view ?? 'low top-down',
    no_background: true,
    ...(opts.seed !== undefined ? { seed: Number(opts.seed) } : {}),
  }
  const start = await call('/generate-8-rotations-v2', body)
  const jobId = start.background_job_id ?? start.job_id ?? start.id
  const done = jobId ? await waitJob(jobId) : start
  const lr8 = done.last_response ?? done
  const imgs = collectImages(lr8.images ?? lr8)
  const quant = lr8.quantized_images ? collectImages(lr8.quantized_images) : []
  fs.mkdirSync(outDir, { recursive: true })
  quant.slice(0, 8).forEach((b, k) => fs.writeFileSync(path.join(outDir, `${id}__${DIRS[k]}__q.png`), b64ToPng(b)))
  fs.writeFileSync(path.join(outDir, `${id}.rot8.json`), JSON.stringify({ request: { ...body, reference_image: { width: ref.width, height: ref.height, from: inFile } }, job: jobId, usage: done.usage ?? done.last_response?.usage ?? null, keys: Object.keys(done.last_response ?? done) }, null, 2))
  imgs.slice(0, 8).forEach((b, k) => fs.writeFileSync(path.join(outDir, `${id}__${DIRS[k]}.png`), b64ToPng(b)))
  const usage = (done.last_response ?? done).billing_usage ?? null
  console.log(`${id}: ${imgs.length} 枚 → ${outDir} usage=${JSON.stringify(usage)}`)
}

async function edit(inFile, outFile, opts) {
  const ref = imgRef(inFile)
  const size = Number(opts.size ?? ref.width)
  const n = Math.max(1, Number(opts.n ?? 1))
  const body = {
    method: 'edit_with_text',
    edit_images: Array.from({ length: n }, () => ref),
    image_size: { width: size, height: size },
    description: String(opts.text ?? ''),
    no_background: true,
    ...(opts.seed !== undefined ? { seed: Number(opts.seed) } : {}),
  }
  const start = await call('/edit-images-v2', body)
  const jobId = start.background_job_id ?? start.job_id ?? start.id
  const done = jobId ? await waitJob(jobId) : start
  const imgs = collectImages(done.last_response ?? done)
  fs.mkdirSync(path.dirname(outFile), { recursive: true })
  imgs.forEach((b, k) => fs.writeFileSync(k === 0 ? outFile : outFile.replace(/\.png$/, `__${k}.png`), b64ToPng(b)))
  const usage = (done.last_response ?? done).billing_usage ?? (done.last_response ?? done).usage ?? null
  fs.writeFileSync(outFile.replace(/\.png$/, '.edit.json'), JSON.stringify({ request: { ...body, edit_images: `${n}× ${inFile}` }, job: jobId, usage, keys: Object.keys(done.last_response ?? done) }, null, 2))
  console.log(`${path.basename(outFile)}: ${imgs.length} 枚 usage=${JSON.stringify(usage)}`)
}

/** 別々の絵を1回で直す (同じ大きさだけ。64以下は16枚・65〜80は9枚・81〜128は4枚まで)。出力は入力と同じ順で <outdir>/<元の名前>.png */
async function editMany(outDir, files, opts) {
  const refs = files.map(imgRef)
  const size = Number(opts.size ?? refs[0].width)
  const body = {
    method: 'edit_with_text',
    edit_images: refs,
    image_size: { width: size, height: size },
    description: String(opts.text ?? ''),
    no_background: true,
    ...(opts.seed !== undefined ? { seed: Number(opts.seed) } : {}),
  }
  const start = await call('/edit-images-v2', body)
  const jobId = start.background_job_id ?? start.job_id ?? start.id
  const done = jobId ? await waitJob(jobId) : start
  const lr = done.last_response ?? done
  const imgs = collectImages(lr.images ?? lr)
  fs.mkdirSync(outDir, { recursive: true })
  files.forEach((f, k) => { if (imgs[k]) fs.writeFileSync(path.join(outDir, path.basename(f)), b64ToPng(imgs[k])) })
  const usage = lr.billing_usage ?? lr.usage ?? null
  fs.writeFileSync(path.join(outDir, `batch-${Date.now()}.json`), JSON.stringify({ files, text: body.description, job: jobId, usage, got: imgs.length, keys: Object.keys(lr) }, null, 2))
  console.log(`${files.length} 枚を送って ${imgs.length} 枚 usage=${JSON.stringify(usage)}`)
}

// ---- HD-2D 見本の副命令 (2026-09-30 P05) ----
// balance: 月の生成枠 (subscription) と買い足しのクレジット (credits) を出す。生成はしない。
// tilespro / mapobj / order は下の「HD-2D 見本」の節。

/** GET /balance。枠の残り回数を返す (v1 の balance は USD しか返さない) */
async function balanceV2(quiet = false) {
  const r = await call('/balance', null, 'GET')
  const sub = r.subscription ?? {}
  if (!quiet) console.log(`枠: ${sub.generations ?? '?'}/${sub.total ?? '?'} (${sub.plan ?? '?'} ${sub.status ?? '?'})  クレジット: ${r.credits?.usd ?? '?'} USD`)
  return { generations: Number(sub.generations ?? NaN), total: Number(sub.total ?? NaN), usd: Number(r.credits?.usd ?? NaN) }
}

/** 生成物の URL (認証なし) を取ってきて保存する */
async function download(url, file) {
  for (let attempt = 1; ; attempt++) {
    const res = await fetch(url)
    if (res.ok) {
      fs.mkdirSync(path.dirname(file), { recursive: true })
      fs.writeFileSync(file, Buffer.from(await res.arrayBuffer()))
      return
    }
    if (attempt < 4) { await new Promise((r) => setTimeout(r, attempt * 5000)); continue }
    throw new Error(`GET ${url.slice(0, 80)}… → HTTP ${res.status}`)
  }
}

/** 状態を問い合わせる GET を、423 (まだ作っている) の間だけ待って繰り返す (5秒おき・最大10分) */
async function pollUntilReady(pathname) {
  for (let i = 0; i < 120; i++) {
    const res = await fetch(API + pathname, { headers: { Authorization: 'Bearer ' + token() } })
    const text = await res.text()
    if (res.ok) {
      const j = JSON.parse(text)
      if (j.status && j.status !== 'completed' && j.status !== 'succeeded') {
        if (j.status === 'failed') throw new Error(`${pathname} failed: ${text.slice(0, 400)}`)
        await new Promise((r) => setTimeout(r, 5000)); continue
      }
      return j
    }
    if (res.status === 423 || res.status === 429 || res.status >= 500) { await new Promise((r) => setTimeout(r, 5000)); continue }
    throw new Error(`GET ${pathname} → HTTP ${res.status}: ${text.slice(0, 400)}`)
  }
  throw new Error(`${pathname} timeout`)
}

/** 発注の禁止語 (docs/pixellab/hd2d-act1/README.md・memory pixellab-stage-lessons)。
 *  描写にあると PixelLab が木・月・風景・段丘・緑の土を描き足す。語の境界で見る ("dirty" は引っかけない) */
const FORBIDDEN_DEFAULT = ['forest', 'night', 'moonlight', 'scene', 'moss green', 'dirt']
function forbiddenHits(text, words) {
  const t = String(text ?? '').toLowerCase()
  return (words ?? FORBIDDEN_DEFAULT).filter((w) => new RegExp(`(^|[^a-z])${w.toLowerCase().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}([^a-z]|$)`).test(t))
}

/** create-tiles-pro。<outdir>/<id>_<k>.png に並びの順で保存し、<id>.tilespro.json に記録 */
async function tilesPro(outDir, o) {
  const id = o.id ?? `tiles_${Date.now()}`
  const body = {
    description: String(o.text ?? o.description ?? ''),
    tile_type: o.type ?? o.tile_type ?? 'square_topdown',
    tile_size: Number(o.size ?? o.tile_size ?? 64),
    outline_mode: o.outlineMode ?? o['outline-mode'] ?? o.outline_mode ?? 'segmentation',
  }
  if (o.view ?? o.tile_view) body.tile_view = o.view ?? o.tile_view
  if (o.angle !== undefined || o.tile_view_angle !== undefined) body.tile_view_angle = Number(o.angle ?? o.tile_view_angle)
  if (o.depth !== undefined || o.tile_depth_ratio !== undefined) body.tile_depth_ratio = Number(o.depth ?? o.tile_depth_ratio)
  if (o.seed !== undefined) body.seed = Number(o.seed)
  if (o.style) {
    const files = Array.isArray(o.style) ? o.style : String(o.style).split(',')
    body.style_images = files.map((f) => { const r = imgRef(f); return { base64: r.image.base64, width: r.width, height: r.height } })
  }
  const start = await call('/create-tiles-pro', body)
  const tileId = start.tile_id
  const done = await pollUntilReady(`/tiles-pro/${tileId}`)
  const urls = done.storage_urls ?? {}
  const keys = Object.keys(urls).sort((a, b) => (Number(a.replace(/\D+/g, '')) || 0) - (Number(b.replace(/\D+/g, '')) || 0))
  const saved = []
  for (let k = 0; k < keys.length; k++) {
    const file = path.join(outDir, `${id}_${k}.png`)
    await download(urls[keys[k]], file)
    saved.push({ key: keys[k], file, ...pngSize(file) })
  }
  const usage = done.usage ?? start.usage ?? null
  fs.mkdirSync(outDir, { recursive: true })
  fs.writeFileSync(path.join(outDir, `${id}.tilespro.json`), JSON.stringify({ id, request: { ...body, style_images: body.style_images ? `${body.style_images.length} 枚` : undefined }, tile_id: tileId, job: start.background_job_id, kind: done.kind, usage, saved, at: new Date().toISOString() }, null, 2))
  console.log(`${id}: ${saved.length} 枚 (${saved.map((s) => `${s.width}×${s.height}`).join(' ')}) usage=${JSON.stringify(usage)}`)
  return { saved, usage }
}

/** map-objects。<out.png> と <out>.mapobj.json */
async function mapObject(outFile, o) {
  const w = Number(o.w ?? o.width ?? (Array.isArray(o.size) ? o.size[0] : o.size) ?? 128)
  const h = Number(o.h ?? o.height ?? (Array.isArray(o.size) ? o.size[1] : o.size) ?? w)
  const body = {
    description: String(o.text ?? o.description ?? ''),
    image_size: { width: w, height: h },
    view: o.view ?? 'side',
    outline: o.outline ?? 'selective outline',
    shading: o.shading ?? 'medium shading',
    detail: o.detail ?? 'high detail',
    text_guidance_scale: Number(o.guidance ?? 8),
  }
  if (o.seed !== undefined) body.seed = Number(o.seed)
  if (o.color) body.color_image = { type: 'base64', base64: fs.readFileSync(o.color).toString('base64'), format: 'png' }
  const start = await call('/map-objects', body)
  const objId = start.object_id
  const done = await pollUntilReady(`/map-objects/${objId}`)
  if (!done.download_url) throw new Error(`map-object ${objId}: download_url が無い ${JSON.stringify(done).slice(0, 300)}`)
  await download(done.download_url, outFile)
  const usage = done.usage ?? start.usage ?? null
  const sz = pngSize(outFile)
  fs.writeFileSync(outFile.replace(/\.png$/, '.mapobj.json'), JSON.stringify({ request: { ...body, color_image: body.color_image ? o.color : undefined }, object_id: objId, job: start.background_job_id, usage, size: sz, at: new Date().toISOString() }, null, 2))
  console.log(`${path.basename(outFile)}: ${sz.width}×${sz.height} usage=${JSON.stringify(usage)}`)
  return { usage }
}

/** v1 の pixflux (1枚1生成・400×400 まで・背景なしは面積≈3万pxまで)。既知の道 (96 で作って中央 64 を切る) の口。
 *  v2 の pixflux は negative_description が効かない (Deprecated) ので v1 を呼ぶ。<out.png> と <out>.pixellab.json */
async function pixflux(outFile, o) {
  const [w, h] = Array.isArray(o.size) ? o.size : [Number(o.w ?? o.size ?? 64), Number(o.h ?? o.size ?? 64)]
  const body = {
    description: String(o.text ?? o.description ?? ''),
    negative_description: String(o.negative ?? ''),
    image_size: { width: w, height: h },
    text_guidance_scale: Number(o.guidance ?? 8),
    no_background: o.no_background ?? true,
  }
  for (const k of ['outline', 'shading', 'detail', 'view', 'direction', 'isometric']) if (o[k] !== undefined) body[k] = o[k]
  if (o.seed !== undefined) body.seed = Number(o.seed)
  const r = await call('https://api.pixellab.ai/v1/generate-image-pixflux', body)
  fs.mkdirSync(path.dirname(outFile), { recursive: true })
  fs.writeFileSync(outFile, b64ToPng(r.image.base64))
  fs.writeFileSync(outFile.replace(/\.png$/, '.pixellab.json'), JSON.stringify({ id: o.id, engine: 'pixflux', request: body, usage: r.usage, at: new Date().toISOString() }, null, 2))
  console.log(`${path.basename(outFile)}: ${w}×${h} usage=${JSON.stringify(r.usage)}`)
  return { usage: r.usage }
}

/** 発注書 (docs/pixellab/hd2d-act1/*.json) を順に作る。
 *  { "forbidden": [...], "defaults": {...}, "items": [ { "id", "kind": "tilespro"|"mapobj"|"edit", "out" (mapobj・edit は PNG / tilespro はフォルダ), "text", ... } ] }
 *  --only a,b  --force (既にある物も作り直す)  --dry (送らずに禁止語の検査と内容の表示だけ)
 *  --budget N (この回で使ってよい生成数の上限。1つ作る前に残りと見積りを比べ、足りなければ止める)
 *  --reserve N (枠の残りがこれを切ったら止める。既定 50)
 *  使った回数は <発注書>.log.jsonl に1行ずつ足す */
async function runOrder(orderFile, o) {
  const m = JSON.parse(fs.readFileSync(orderFile, 'utf-8'))
  const words = m.forbidden ?? FORBIDDEN_DEFAULT
  const only = o.only ? String(o.only).split(',') : null
  const budget = o.budget !== undefined ? Number(o.budget) : Infinity
  const reserve = Number(o.reserve ?? 50)
  const logFile = orderFile.replace(/\.json$/, '.log.jsonl')
  let spent = 0
  let bad = 0
  for (const item of m.items) {
    const it = { ...(m.defaults ?? {}), ...(m.defaultsByKind?.[item.kind] ?? {}), ...item }
    const hits = forbiddenHits(it.text, words)
    if (hits.length && !it.allowForbidden) { console.error(`禁止語 ${hits.join(', ')}: ${it.id}`); bad++ }
  }
  if (bad) { console.error(`禁止語が ${bad} 件。直すか allowForbidden を付ける`); process.exit(3) }
  for (const item of m.items) {
    if (only && !only.includes(item.id)) continue
    const it = { ...(m.defaults ?? {}), ...(m.defaultsByKind?.[item.kind] ?? {}), ...item }
    const out = path.resolve(it.out)
    const exists = it.kind === 'tilespro' ? fs.existsSync(path.join(out, `${it.id}.tilespro.json`)) : fs.existsSync(out)
    if (exists && !o.force) { console.log(`skip (既にある): ${it.id}`); continue }
    // 見積り: tiles-pro は 20〜25 (公式 docs/tools/create-tiles-pro)・edit-images-v2 は 20・pixflux は 1・map-objects は未公表 (試しで測る)
    const est = Number(it.estimate ?? (it.kind === 'tilespro' ? 25 : it.kind === 'edit' ? 20 : 1))
    if (o.dry) { console.log(`[dry] ${it.kind} ${it.id} (見積り ${est}) → ${it.out}\n      ${it.text}`); continue }
    if (spent + est > budget) { console.error(`この回の上限 ${budget} に届くので止める (使用 ${spent})`); break }
    const bal = await balanceV2(true)
    if (Number.isFinite(bal.generations) && bal.generations - est < reserve) { console.error(`枠の残り ${bal.generations} が予備 ${reserve}＋見積り ${est} を切るので止める`); break }
    let usage = null
    try {
      if (it.kind === 'tilespro') usage = (await tilesPro(out, it)).usage
      else if (it.kind === 'mapobj') usage = (await mapObject(out, it)).usage
      else if (it.kind === 'pixflux') usage = (await pixflux(out, it)).usage
      else if (it.kind === 'edit') { await edit(path.resolve(it.in), out, it); usage = null }
      else throw new Error(`kind が分からない: ${it.kind}`)
    } catch (e) {
      fs.appendFileSync(logFile, JSON.stringify({ id: it.id, kind: it.kind, error: String(e.message ?? e).slice(0, 500), at: new Date().toISOString() }) + '\n')
      // 4xx (402 枠切れ・422 引数・401 鍵) は続けても無駄なので止める
      if (/HTTP 4\d\d/.test(String(e.message))) { console.error(`止める: ${e.message}`); process.exit(4) }
      console.error(`失敗 (続ける): ${it.id}: ${e.message}`); continue
    }
    const after = await balanceV2(true)
    // 使った数は応答の usage を正とする (枠の残りは数秒遅れて減るので差では測れない。2026-09-30 実測)
    const used = usage?.type === 'generations' && Number.isFinite(Number(usage.generations)) ? Number(usage.generations)
      : Number.isFinite(bal.generations) && Number.isFinite(after.generations) && bal.generations > after.generations ? bal.generations - after.generations : est
    spent += used
    fs.appendFileSync(logFile, JSON.stringify({ id: it.id, kind: it.kind, used, left: after.generations, usage, at: new Date().toISOString() }) + '\n')
    console.log(`  使った ${used} (この回 ${spent}・枠の残り ${after.generations})`)
  }
  console.log(`この回の合計 ${spent} 生成`)
}

const { pos, opts } = args()
const cmd = pos[0]
if (cmd === 'rot8') await rot8(pos[1], pos[2], opts)
else if (cmd === 'edit') await edit(pos[1], pos[2], opts)
else if (cmd === 'editmany') await editMany(pos[1], pos.slice(2), opts)
else if (cmd === 'balance') await balanceV2()
else if (cmd === 'tilespro') await tilesPro(pos[1], opts)
else if (cmd === 'mapobj') await mapObject(pos[1], opts)
else if (cmd === 'order') await runOrder(pos[1], opts)
else if (cmd === 'forbidden') { const h = forbiddenHits(pos.slice(1).join(' ')); console.log(h.length ? `禁止語: ${h.join(', ')}` : '禁止語なし'); process.exit(h.length ? 3 : 0) }
else { console.error('usage: pixellab-v2.mjs rot8 <in.png> <outdir> [--id X] [--size N] [--view v] [--seed N] | edit <in.png> <out.png> --text "..." [--size N] [--seed N] [--n K] | editmany <outdir> <png...> --text "..." | balance | tilespro <outdir> --text "..." [--id X] [--size 64] [--view v|--angle N] [--outline-mode segmentation] [--style a.png,b.png] [--seed N] | mapobj <out.png> --text "..." [--w N --h N] [--view side] [--seed N] | order <発注書.json> [--only a,b] [--force] [--dry] [--budget N] [--reserve 50] | forbidden <文>'); process.exit(1) }
