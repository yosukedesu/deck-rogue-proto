#ifndef DECKROGUE_TRIPLE_HORN_RECEIVE
#define DECKROGUE_TRIPLE_HORN_RECEIVE
// Exact selected contract lights only. Global zero is the unchanged default.
float _TripleHornLightCount;
float4 _TripleHornLights[2];
float TripleHornReceive(float4 lp) {
 if(lp.w<.5||_TripleHornLightCount<.5)return 1;
 float result=1;
 [unroll]for(int i=0;i<2;i++){float3 delta=lp.xyz-_TripleHornLights[i].xyz;if(i<_TripleHornLightCount&&dot(delta,delta)<1e-4)result=_TripleHornLights[i].w;}
 return result;
}
#endif
