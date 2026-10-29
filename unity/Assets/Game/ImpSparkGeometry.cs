using System;
namespace DeckRogue.Game {
 // Selected289's two appended pressure grids. Original Fire sampler/flight/light remain owned by the set.
 internal sealed class ImpSparkGeometry {
  public readonly FireContinuousGeometry.MeshData Hot=FireContinuousGeometry.Create("projectile").Flight,Cool=FireContinuousGeometry.Create("projectile").Flight;
  public readonly FireContinuousGeometry.MeshData[] Batches;
  public bool Committed;
  public ImpSparkGeometry(FireContinuousGeometry.Buffer original){Batches=new FireContinuousGeometry.MeshData[8];Array.Copy(original.Batches,Batches,6);Batches[6]=Hot;Batches[7]=Cool;}
  static double Smooth(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
  public void Clear(){Committed=false;Array.Clear(Hot.Alpha,0,165);Array.Clear(Cool.Alpha,0,165);}
  public void Sample(FireContinuousGeometry.Buffer old){
   Array.Clear(Hot.Alpha,0,165);Array.Clear(Cool.Alpha,0,165);double t=old.VisualAge;
   if(!double.IsFinite(t)||t<0||!Committed)return;
   for(int k=0;k<old.TongueCount;k++)Array.Clear(old.Batches[old.TongueOffset+k].Alpha,0,99);
   double ax=-old.Source.X,ay=-old.Source.Y,len=Math.Sqrt(ax*ax+ay*ay);if(len>0){ax/=len;ay/=len;}
   double open=.82+.18*Smooth(t/.04),consume=Smooth((t-.045)/.16),heat=1-Smooth((t-.035)/.145),cooling=Smooth((t-.065)/.04)*(1-Smooth((t-.12)/.10));
   for(int layer=0;layer<2;layer++){
    var m=layer==0?Hot:Cool;double width=(layer==0?.82:.74)*open,height=layer==0?.62:.52,opacity=layer==0?heat*.88:cooling*.34;
    for(int i=0;i<33;i++)for(int j=0;j<5;j++){
     int n=i*5+j;double u=i/32.0,v=j/4.0,along=(u-.38)*width,across=(.58-v)*height,edge=Math.Sqrt((u-.38)*(u-.38)+(v-.58)*(v-.58));
     m.Vertices[n]=new FireContinuousGeometry.Point(ax*along-ay*across,ay*along+ax*across,.025+(layer==1?.004:0));
     m.UV[n]=new FireContinuousGeometry.Uv(u,1-v);m.Alpha[n]=opacity*(1-consume*Smooth((edge-.12)/.55));m.Gain[n]=layer==0?1:.55;m.Erosion[n]=0;
    }
   }
  }
 }
}
