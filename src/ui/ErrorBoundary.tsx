// ui/ErrorBoundary.tsx — 描画クラッシュ時の白画面防止 (プレイテスト期の保険)。
// エラー内容を画面に出すことで、テスターの「何も表示されない」を「何が壊れたか」の報告に変える。

import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'

interface State {
  error: Error | null
}

export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('描画クラッシュ:', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      return (
        <div style={{ padding: 24, color: '#e07a5f', fontFamily: 'monospace' }}>
          <h2>⚠️ 画面の描画でエラーが発生しました</h2>
          <p style={{ color: '#d9e2da' }}>
            このメッセージのスクリーンショットを開発者に送ってください。
          </p>
          <pre style={{ whiteSpace: 'pre-wrap', fontSize: 12 }}>
            {this.state.error.message}
            {'\n\n'}
            {this.state.error.stack}
          </pre>
          <button
            style={{ padding: '8px 16px', fontSize: 14 }}
            onClick={() => location.reload()}
          >
            リロードして最初から
          </button>
        </div>
      )
    }
    return this.props.children
  }
}
