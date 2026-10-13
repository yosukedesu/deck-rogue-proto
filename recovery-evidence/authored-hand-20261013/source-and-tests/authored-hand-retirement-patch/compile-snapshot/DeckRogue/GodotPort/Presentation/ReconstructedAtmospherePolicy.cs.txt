using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.Presentation;

// Reconstructed 2026-10-12 from accepted visual references. Not recovered b05a0da source.
internal static class ReconstructedAtmospherePolicy
{
    internal const string Version = "reconstructed-atmosphere-20261012-r4";
    internal static bool Allows(int act, string renderer, bool baseline, bool profileMatches) =>
        act >= 1 && act <= 3 && renderer == "gl_compatibility" && !baseline && profileMatches;
    internal static readonly IReadOnlyDictionary<string,string> SourceHashes = new Dictionary<string,string>
    {
        ["assets/Stage/act2_layout.json"] = "bd682de2dc7c6fcf8779ce912e83082984849a0fd19319318e9c561a2ad7d595",
        ["assets/Stage/act3_layout.json"] = "6b763320784169d2c0fa132b11d84011ab51b90eafdbe55e6b6769ba6238cfa2",
        ["assets/Stage/look_act1.json"] = "ce840514830a2e701ac52b95b7a504a948eedf5a6333f0f9a0e220709a065d48",
        ["assets/Stage/look_act1.char.json"] = "f30744a0079e03679dbb2bf9cb9a2335294f20256553b02c95c51fff4f6b187b",
        ["assets/Stage/look_act1.dof.json"] = "76bbe3a98e2d70e9b134977a61a4b9c63f01c3d9582237077ab00b5ce31bc6b9",
        ["assets/Stage/look_act1.phone.json"] = "45a721343c4f347fa9dff10926211e431896717d0d336aa6f4e4d7af7ac5b848",
        ["assets/Stage/look_act2.json"] = "128997be5318f2a3c6c3a395ed787af52c0de95a4c275caadd3cc3084aa62dbb",
        ["assets/Stage/look_act2.char.json"] = "119c9722d11800084571573fccce83a52fa369eb3193b56be1e44b2045dc16a0",
        ["assets/Stage/look_act2.dof.json"] = "57daa88fe0da18138dacf76c42716b412044c0419d22c40358b7eb9f56eaed53",
        ["assets/Stage/look_act2.phone.json"] = "c9a65991c7a7348b9a8fab09701b8d2f82ba6b7f3e7627c863bbe0f00d474a9c",
        ["assets/Stage/look_act3.json"] = "10ab1cb9066b33dc099617bd093228f501ea5c42a4f190d93ddbccce8441da3f",
        ["assets/Stage/look_act3.char.json"] = "25701b41ef19b057efae43def71cfc1a3d792cd6a189051819f9400545760209",
        ["assets/Stage/look_act3.dof.json"] = "b593634e055e0a0456125d02413a3e7d5411ecc4aa1584d58a77d40966ea050b",
        ["assets/Stage/look_act3.phone.json"] = "5d593ee0c41c9a7f486976715406cea13661faadefb87680ac36087fe8162513",
        ["assets/Stage/look_act1_preview_night.json"] = "4103d41c65c72ae494253d8e2aaa398aa645bff1c5106958b00d9ddd45e00f6d",
        ["assets/Stage/look_act1_integrated.json"] = "6b97d2bd6e997d1a107e94fd49219cafd296b4c47790905621e560a0becec8a8",
    };
}
