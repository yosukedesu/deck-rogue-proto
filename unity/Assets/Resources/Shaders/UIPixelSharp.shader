// UIPixelSharp — UI のドット絵を「シャープ・バイリニア」で描く (2026-09-29 p25)。
// 中身は組み込みの UI/Default (Unity 6) の写し: Stencil (Mask)・ColorMask・_ClipRect (RectMask2D のスクロール一覧)・アルファクリップ・
// 頂点色 (出せない札の灰・組めないギアの α)・乗算済みの出力。違うのはテクスチャの読み方だけ:
//   ドットの中心は平らに (最近傍と同じ色)、ドットの境目の「画面の 1px」(1〜1.5倍は 0.5px・ほぼ等倍は最近傍。2026-09-30 F49) だけを隣のドットと混ぜる。
// → 手札の挿絵 (PC 1.84倍・スマホ 2.63倍)・意図の絵 (1.375倍)・ギア (1.5倍) のような非整数倍でも、傾けた札 (±3〜6°) でも、
//   ドットの太さが見た目でそろい、なぞる・持ち上げる途中の縁がちらつかない。
// ・テクスチャの filterMode は Point のまま (4点を最近傍で読んで手で混ぜる)。取り込みの規約 (ArtImporter) と舞台の板は触らない。
// ・軸に沿った整数倍 (PC 1920×1080 の報酬・店の札 2倍、16px の絵の等倍など) は最近傍のまま＝今までと同じ画素
//   (半画素ずれて置かれても境目がにじまない)。
// ・縮小 (1倍未満) はふつうのバイリニア (1ドットの線を飛ばさない)。
// ・_MainTex_TexelSize が来ない時は最近傍 (＝UI/Default と同じ見た目)。
// 当てる側は UiKit.PixelArt (Point フィルタでテクスチャ丸ごとのスプライトだけ)。
Shader "DeckRogue/UIPixelSharp"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // ddx/ddy と tex2Dlod を使うので 3.0 (UI/Default は 2.0)。Android は GLES3/Vulkan なので問題ない
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask          : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // (1/w, 1/h, w, h)。テクスチャの寸法
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if (!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            // テクセル (整数の番地) を1つ最近傍で読み、色を乗算済みにする (透明なドットの黒が縁ににじまないように)。
            // 番地は絵の中に留める (wrapMode が Repeat のコード生成の絵でも、右端で左端のドットを拾わない)
            float4 TapPm(float2 texel)
            {
                texel = clamp(texel, 0.0, _MainTex_TexelSize.zw - 1.0);
                float4 c = tex2Dlod(_MainTex, float4((texel + 0.5) * _MainTex_TexelSize.xy, 0, 0));
                c.rgb *= c.a;
                return c;
            }

            float4 SampleSharp(float2 uv)
            {
                float2 size = _MainTex_TexelSize.zw;
                float2 px = uv * size;                                  // テクセル単位の位置
                float2 dx = ddx(px);
                float2 dy = ddy(px);
                float2 tpp = max(sqrt(dx * dx + dy * dy), 1e-4);        // 画面の 1px あたりのテクセル数 (傾けても長さは正しい)
                float2 k = 1.0 / tpp;                                   // 倍率 (1テクセルが画面の何 px か)
                float2 sc = max(k, 1.0);                                // 縮小はふつうのバイリニア
                float2 d = frac(px) - 0.5;
                // 境目をなじませる幅 (画面の px): 1〜1.5倍は 0.5px・2倍以上は 1px・縮小は 1px (＝ふつうのバイリニア)。
                // 2026-09-30 F49: 旧は全部 1px で、2倍に届かない絵 (意図 1.375倍・ギア 1.5倍) は1ドットの大半が中間色になり、墨の輪郭が 69% まで薄れた
                float2 t = lerp(1.0, clamp(k - 1.0, 0.5, 1.0), step(1.0, k));
                float2 hw = 0.5 * t / sc;                               // 境目の半幅 (テクセル)
                float2 r = 0.5 - hw;                                    // ドットの中の平らな範囲 (半幅。テクセル単位)
                float2 f = (d - clamp(d, -r, r)) * (0.5 / hw) + 0.5;    // 平らな所は 0.5 (＝中心)、境目だけ 0→1 (d=±0.5 で 0/1＝隣とつながる)
                // 軸に沿った整数倍とほぼ等倍 (0.98〜1.1 倍。スマホのからくりのトークン 1.04倍) は最近傍 (＝今までと同じ画素。半画素ずれて置かれても境目をにじませない)
                bool aligned = (abs(dx.y) + abs(dy.x)) < 1e-3 * (tpp.x + tpp.y);
                bool integral = all(abs(k - round(k)) < 0.02) && all(k > 0.98);
                bool nearOne = all(k > 0.98) && all(k < 1.1);
                f = (aligned && (integral || nearOne)) ? float2(0.5, 0.5) : f;
                float2 q = floor(px) + f - 0.5;
                float2 i0 = floor(q);
                float2 w = q - i0;
                float4 c00 = TapPm(i0);
                float4 c10 = TapPm(i0 + float2(1, 0));
                float4 c01 = TapPm(i0 + float2(0, 1));
                float4 c11 = TapPm(i0 + float2(1, 1));
                float4 c = lerp(lerp(c00, c10, w.x), lerp(c01, c11, w.x), w.y);
                c.rgb /= max(c.a, 1e-4);                                // 乗算済みを戻す (以下は UI/Default と同じ式に乗せる)
                // テクセルの寸法が来ていない時は最近傍 (UI/Default と同じ見た目)
                return size.x >= 1.0 ? c : tex2Dlod(_MainTex, float4(uv, 0, 0));
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                half4 color = IN.color * ((half4)SampleSharp(IN.texcoord) + _TextureSampleAdd);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
}
