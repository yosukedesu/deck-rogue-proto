using System;
using UnityEngine;
using G=DeckRogue.Game.LocalMaterialProfileGeometry;
namespace DeckRogue.Game {
 // Selected326 private body/core. Original local buffer supplies unchanged particles and light envelopes.
 internal sealed class LightBurstGeometry {
  internal readonly G.MeshData Main=new G.MeshData(64,32,41),Core=new G.MeshData(4,2,42);
  internal Vector2 Axis=Vector2.right;internal bool Committed;
  internal LightBurstGeometry(){for(int q=0;q<16;q++){int n=q*6,v=q*4;Main.Triangles[n]=v;Main.Triangles[n+1]=v+1;Main.Triangles[n+2]=v+2;Main.Triangles[n+3]=v;Main.Triangles[n+4]=v+2;Main.Triangles[n+5]=v+3;}Array.Copy(new[]{0,1,2,0,2,3},Core.Triangles,6);var uv=new[]{new G.Uv(0,0),new G.Uv(1,0),new G.Uv(1,1),new G.Uv(0,1)};Array.Copy(uv,Core.UV,4);for(int i=0;i<64;i++){Main.Gain[i]=2.15;Main.Role[i]=1;}for(int i=0;i<4;i++){Core.Gain[i]=2.7;Core.Role[i]=3;}}
  internal void Configure(Vector3 source){var a=new Vector2(-source.x,-source.y);Axis=a.sqrMagnitude>.0001f?a.normalized:Vector2.right;}
  G.Point At(double along,double across,double z)=>new G.Point(Axis.x*along-Axis.y*across,Axis.y*along+Axis.x*across,z);
  internal void Clear(){Committed=false;Array.Clear(Main.Alpha,0,64);Array.Clear(Core.Alpha,0,4);}
  internal void Sample(double t,bool committed){
   Clear();Committed=committed;if(!committed||!G.Finite(t)||t<0||t>=.172)return;
   double fade=1-G.Smooth((t-.035)/.137),front=G.Smooth((t-.025)/.115)*.84;
   void Vertex(int n,double u,double side){double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.70);double width=(.009+.16*belly*(1-.4*u))*(1-.32*G.Smooth((t-.04)/.12));double along=u<=.625?-.48+.592*u:-.11+.145*((u-.625)/.375);double across=.035*Math.Sin(Math.PI*u)+side*width*(side>0?1:.55);Main.Vertices[n]=At(along,across,.030);Main.UV[n]=new G.Uv(u,(side+1)*.5);Main.Alpha[n]=.74*fade*G.Smooth((u-front)/.14)*(.45+.55*belly)*(side==0?1:side>0?.48:.28);}
   for(int k=0;k<8;k++)for(int j=0;j<2;j++){int n=(k*2+j)*4;double u=k/8.0,v=(k+1)/8.0;Vertex(n,u,j-1);Vertex(n+1,v,j-1);Vertex(n+2,v,j);Vertex(n+3,u,j);}
   Core.Vertices[0]=At(-.09,-.018,.080);Core.Vertices[1]=At(.035,-.010,.080);Core.Vertices[2]=At(.020,.015,.080);Core.Vertices[3]=At(-.075,.022,.080);for(int i=0;i<4;i++)Core.Alpha[i]=.62*(1-G.Smooth((t-.014)/.060));
  }
 }
}
