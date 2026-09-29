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
    const res = await fetch(API + pathname, {
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

const { pos, opts } = args()
const cmd = pos[0]
if (cmd === 'rot8') await rot8(pos[1], pos[2], opts)
else if (cmd === 'edit') await edit(pos[1], pos[2], opts)
else if (cmd === 'editmany') await editMany(pos[1], pos.slice(2), opts)
else { console.error('usage: pixellab-v2.mjs rot8 <in.png> <outdir> [--id X] [--size N] [--view v] [--seed N] | edit <in.png> <out.png> --text "..." [--size N] [--seed N] [--n K]'); process.exit(1) }
