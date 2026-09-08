// scripts/sync-unity-data.ts — src/data/*.json を Unity の StreamingAssets へ複製する (共通資産は無変更で搭載 = docs/unity-port.md §0)。
// 使い方: npm run unity:sync   (check を付けると差分があれば非0で終了 = CI/テスト用)
import { copyFileSync, mkdirSync, readdirSync, readFileSync, existsSync } from 'node:fs'
import { join } from 'node:path'

const SRC = 'src/data'
const DST = 'unity/Assets/StreamingAssets/data'
const DST2 = 'unity/Assets/Resources/Data'   // Android (APK 内) は StreamingAssets を File で読めないので TextAsset としても持つ (2026-09-09)
const check = process.argv.includes('check')
mkdirSync(DST, { recursive: true })
mkdirSync(DST2, { recursive: true })
let diff = 0
for (const f of readdirSync(SRC).filter((x) => x.endsWith('.json'))) {
  const a = readFileSync(join(SRC, f), 'utf-8')
  for (const dst of [DST, DST2]) {
    const b = existsSync(join(dst, f)) ? readFileSync(join(dst, f), 'utf-8') : null
    if (a !== b) {
      diff++
      if (!check) copyFileSync(join(SRC, f), join(dst, f))
      console.log(`${check ? 'DIFF' : 'copied'} ${dst}/${f}`)
    }
  }
}
if (check && diff > 0) {
  console.error(`unity/Assets/StreamingAssets/data が src/data と ${diff} 件ずれている: npm run unity:sync`)
  process.exit(1)
}
console.log(check ? 'unity data: 同期済み' : `unity data: ${diff} 件を複製`)
