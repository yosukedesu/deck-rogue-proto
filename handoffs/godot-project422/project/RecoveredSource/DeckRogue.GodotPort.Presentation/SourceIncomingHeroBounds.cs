using System;
using System.Collections.Generic;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceIncomingHeroBounds
{
	public readonly record struct Metadata(string TexturePath, string ImportedPath, string Sha256, Vector2I TextureSize, Rect2 OpaquePixels);

	public enum VerificationMode
	{
		UnsupportedTexture,
		UnknownTexture,
		ImportedPathMismatch,
		DimensionMismatch,
		HashUnavailable,
		HashMismatch,
		VerifiedFirstRead,
		VerifiedCacheHit,
		RejectedCacheHit,
		IdentityChanged,
		CacheCapacityReached
	}

	private readonly record struct Verification(string TexturePath, string ImportedPath, Vector2I TextureSize, bool Verified, Metadata Metadata);

	public const int MaximumCachedIdentities = 256;

	public const float AlphaCutoff = 0.4f;

	private static readonly Dictionary<ulong, Verification> Cache = new Dictionary<ulong, Verification>();

	private static readonly object Gate = new object();

	private static int _hashReadCount;

	private static readonly Dictionary<string, Metadata> Table = new Dictionary<string, Metadata>(StringComparer.Ordinal)
	{
		["res://assets/Art/leaders/anim/leader_blue_attack_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_attack_0.png", "res://.godot/imported/leader_blue_attack_0.png-b18edb6ebe7042043b5fde5ad7cc8d7e.ctex", "157665dfdd6e22b58a6a55af575c4e5137ffdd7a9954dad3fe1f674ab0a6b880", new Vector2I(128, 84), new Rect2(23f, 17f, 70f, 60f)),
		["res://assets/Art/leaders/anim/leader_blue_attack_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_attack_1.png", "res://.godot/imported/leader_blue_attack_1.png-fc2de5f845c97aa634129247f9f7a38c.ctex", "0a712dcdcd5f2a222caf95da71a72ab9ef25c03e0bc690fa912b8b7f1ce6e9f0", new Vector2I(128, 84), new Rect2(29f, 9f, 75f, 68f)),
		["res://assets/Art/leaders/anim/leader_blue_attack_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_attack_2.png", "res://.godot/imported/leader_blue_attack_2.png-41d48fefb7f126b08c2decba606da68c.ctex", "7c5bf2da44ced041fdb4ef767d8851c56f15636821073c5290e7b0be01b5b470", new Vector2I(128, 84), new Rect2(21f, 17f, 92f, 60f)),
		["res://assets/Art/leaders/anim/leader_blue_attack_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_attack_3.png", "res://.godot/imported/leader_blue_attack_3.png-4aa547604f42ced02eba1975f0122abb.ctex", "10d0c153b24c96dab11e52a72a100ad11247b3f08064dd603cb8246158900d6c", new Vector2I(128, 84), new Rect2(33f, 16f, 75f, 61f)),
		["res://assets/Art/leaders/anim/leader_blue_block_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_block_0.png", "res://.godot/imported/leader_blue_block_0.png-2b5cbac24e7db03f78fa2ddb146d2d56.ctex", "9b33550f7d1efaaf5f2abe9bfbf2d56c7f19a1d170d03e7951ad5d5c19438989", new Vector2I(128, 84), new Rect2(25f, 15f, 71f, 62f)),
		["res://assets/Art/leaders/anim/leader_blue_block_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_block_1.png", "res://.godot/imported/leader_blue_block_1.png-870511fec5793e1faa0336f0338d9873.ctex", "0ce8269c0d43ceb308a23e3062259a72e223e36e71fd15a03810c933e6b9eb3d", new Vector2I(128, 84), new Rect2(31f, 15f, 55f, 62f)),
		["res://assets/Art/leaders/anim/leader_blue_block_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_block_2.png", "res://.godot/imported/leader_blue_block_2.png-2ea2b0fe168c07cca32bdc5bcff8caeb.ctex", "631f4366ce83bbda3d57a76477c7aa476d35cd16ee2d0f09d82dab9db7efb83c", new Vector2I(128, 84), new Rect2(31f, 16f, 59f, 61f)),
		["res://assets/Art/leaders/anim/leader_blue_block_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_block_3.png", "res://.godot/imported/leader_blue_block_3.png-9ea4cc7e29d4b855d8f636cd5f04d509.ctex", "0c96144c959352757e57640532c4607e37a7bd456192de60066a1a3d9b61a9ae", new Vector2I(128, 84), new Rect2(27f, 15f, 71f, 62f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_0.png", "res://.godot/imported/leader_blue_idle_0.png-4ab0ee2212fab05f644a45393ee66c4d.ctex", "a0183ff152d85773ba4ed726791deeda640ea43d8034f685455933d5288f53cf", new Vector2I(128, 84), new Rect2(31f, 13f, 65f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_1.png", "res://.godot/imported/leader_blue_idle_1.png-f7cc6175fc54faaa5e0f736e71ff0a9a.ctex", "7e3d35d3f9f463223764dba44d2a37c7fa0151003ebd260defe19cc13de3bb5c", new Vector2I(128, 84), new Rect2(30f, 13f, 66f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_2.png", "res://.godot/imported/leader_blue_idle_2.png-3b2dc66c98c074f75faacfe4f2b36608.ctex", "36f054db346cc0711a2ed4742805557ae6e8feb1a2f54c2dfe1c17f0e997a537", new Vector2I(128, 84), new Rect2(27f, 13f, 69f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_3.png", "res://.godot/imported/leader_blue_idle_3.png-d4d2369726b305aef26858769cea9b81.ctex", "87661f94aec87ab3cd75563a7925ec2f5465ab8d2abcec1a1caa37788a3fd63b", new Vector2I(128, 84), new Rect2(30f, 13f, 66f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_4.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_4.png", "res://.godot/imported/leader_blue_idle_4.png-47d04c672477b409acf746e5114c7874.ctex", "5a7506b027f9f929a75d84446f90545ba895beba10a19ffc12d3e538be26dc4f", new Vector2I(128, 84), new Rect2(30f, 13f, 66f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_5.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_5.png", "res://.godot/imported/leader_blue_idle_5.png-f0b83df9f79313be4045989c8a3aaa3f.ctex", "9218e6eaeae6e85254419d597be6105e592bdd7064e4df2e8a4e2ed5534220a1", new Vector2I(128, 84), new Rect2(29f, 13f, 67f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_6.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_6.png", "res://.godot/imported/leader_blue_idle_6.png-9ba0a66c00d1dbf44f8c8dcbf9472f21.ctex", "f63914db591dbabebd5aef62e1b73ccfaa9e35840f45bdb847e39910447b8b43", new Vector2I(128, 84), new Rect2(29f, 13f, 67f, 64f)),
		["res://assets/Art/leaders/anim/leader_blue_idle_7.png"] = new Metadata("res://assets/Art/leaders/anim/leader_blue_idle_7.png", "res://.godot/imported/leader_blue_idle_7.png-50b4fa80b0226e3eaf8c8a73ab15607d.ctex", "9d9929e1376b8b050ed18828c08264f9d22874df7336ff3d00716b8913656e18", new Vector2I(128, 84), new Rect2(30f, 13f, 66f, 64f)),
		["res://assets/Art/leaders/anim/leader_green_48_attack_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_attack_0.png", "res://.godot/imported/leader_green_48_attack_0.png-4a1333d087d6105000e51d5e151e6740.ctex", "7534b70d23dedb711a2cf46a2cc913851654fd2f80d6c18cc71226b8c619570f", new Vector2I(71, 60), new Rect2(22f, 8f, 28f, 52f)),
		["res://assets/Art/leaders/anim/leader_green_48_attack_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_attack_1.png", "res://.godot/imported/leader_green_48_attack_1.png-5dbc26519059ae231705870d132ebccc.ctex", "d65b99e85d5fc9da5fb6a2a87e01eb1593a050b0bc6acabfb450ca312c67de35", new Vector2I(71, 60), new Rect2(9f, 14f, 41f, 46f)),
		["res://assets/Art/leaders/anim/leader_green_48_attack_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_attack_2.png", "res://.godot/imported/leader_green_48_attack_2.png-65778ecb6549cf7113525bd83d925d77.ctex", "45de58c31ea7318e44889e01046f44a6d4cce576ace556d4511f3a4a722202ea", new Vector2I(71, 60), new Rect2(15f, 10f, 48f, 50f)),
		["res://assets/Art/leaders/anim/leader_green_48_attack_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_attack_3.png", "res://.godot/imported/leader_green_48_attack_3.png-1d2e84b57e33338aca46a002697cc21d.ctex", "f4e25383bb5593e34628b1fa894e5d010a22dd664793c946486540b8c8e827bf", new Vector2I(71, 60), new Rect2(11f, 14f, 51f, 46f)),
		["res://assets/Art/leaders/anim/leader_green_48_block_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_block_0.png", "res://.godot/imported/leader_green_48_block_0.png-21c2db20db45bb6b843a54da4dcf8b24.ctex", "e74a77a7a7f78fe7e469cbee1d5a452e11eca6251af9ba3e9eb3ae4652429884", new Vector2I(71, 60), new Rect2(13f, 13f, 47f, 47f)),
		["res://assets/Art/leaders/anim/leader_green_48_block_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_block_1.png", "res://.godot/imported/leader_green_48_block_1.png-cbc80866fd7c7b774f36a76ddd138351.ctex", "2689481e7d382cee96dbc546f113c924af8cfa96139f97bd941e6ba294116b56", new Vector2I(71, 60), new Rect2(11f, 15f, 49f, 45f)),
		["res://assets/Art/leaders/anim/leader_green_48_block_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_block_2.png", "res://.godot/imported/leader_green_48_block_2.png-cd1ddddb2c687cbaf2be84d0ae8b8891.ctex", "c2eb998cca63dc1fdfcd4cea0662bb51e655aefe4c9a067027bb525db797207a", new Vector2I(71, 60), new Rect2(11f, 15f, 49f, 45f)),
		["res://assets/Art/leaders/anim/leader_green_48_block_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_block_3.png", "res://.godot/imported/leader_green_48_block_3.png-5d724f33305dc33360b66dd40374e1fa.ctex", "b520234ca460333bab22869fe730e376cfd2d422b526c2a4d5ee7d95d86bfe40", new Vector2I(71, 60), new Rect2(11f, 15f, 49f, 45f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_0.png", "res://.godot/imported/leader_green_48_idle_0.png-d5b4428a1deca262fb33c5106e6d9709.ctex", "12de0b1a7aaef16e0598d328062eff997d8d58309fe64de349a7f8450f8b02d2", new Vector2I(71, 60), new Rect2(12f, 12f, 47f, 48f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_1.png", "res://.godot/imported/leader_green_48_idle_1.png-363e3a4b5bd46274d883e282f87ad5ee.ctex", "c552773f43d65f12ecd96dd58c35cd1e523f4b17312c679b61dcf98c92d8b680", new Vector2I(71, 60), new Rect2(12f, 12f, 47f, 48f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_2.png", "res://.godot/imported/leader_green_48_idle_2.png-9c061d3e6b73d546c6907bbf1edb87a5.ctex", "cec8b22a8d69c8d453c2890ab4eb44643ef34b702c0a45068ef85f60688c5adf", new Vector2I(71, 60), new Rect2(13f, 11f, 46f, 49f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_3.png", "res://.godot/imported/leader_green_48_idle_3.png-401d463643d49566883e560b968097d9.ctex", "2c52c3ca34bf10a5b247cd8c6d57833e3fd905841c588c0060f243c8e5f21dce", new Vector2I(71, 60), new Rect2(13f, 10f, 46f, 50f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_4.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_4.png", "res://.godot/imported/leader_green_48_idle_4.png-880e51fe32304f1807f485557bd88031.ctex", "10366ca1729785c9895259d5bcd2158dd896cb3edea3950e5e4f5bad8ce17468", new Vector2I(71, 60), new Rect2(14f, 10f, 45f, 50f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_5.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_5.png", "res://.godot/imported/leader_green_48_idle_5.png-b2f4a620b0a74f7ca33543177608e19b.ctex", "0ae60be5f755edb0ceff039b234ae5b173b92e08bcfb7c32e8790648999c84d8", new Vector2I(71, 60), new Rect2(14f, 10f, 45f, 50f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_6.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_6.png", "res://.godot/imported/leader_green_48_idle_6.png-35de2b3845d66f93b57556e9f738660c.ctex", "97ae886f2a54d91e1c2662335b8dd6a4aadb7a5ef85567c5dd387501e2c935dd", new Vector2I(71, 60), new Rect2(13f, 10f, 46f, 50f)),
		["res://assets/Art/leaders/anim/leader_green_48_idle_7.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_48_idle_7.png", "res://.godot/imported/leader_green_48_idle_7.png-370e6977dee82a7232020c37bec42143.ctex", "7c905bae330ca08f791a238eac8d5658b2aa296f86ca6b28484ac78f001a8b83", new Vector2I(71, 60), new Rect2(12f, 11f, 47f, 49f)),
		["res://assets/Art/leaders/anim/leader_green_attack_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_attack_0.png", "res://.godot/imported/leader_green_attack_0.png-edd4ea76c80256117d394035b16ef1ed.ctex", "9be9b597ae605c2f34a5eea9dfd5145d811f35e62cc0e5e2d659bcbe206ace40", new Vector2I(92, 78), new Rect2(28f, 11f, 37f, 67f)),
		["res://assets/Art/leaders/anim/leader_green_attack_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_attack_1.png", "res://.godot/imported/leader_green_attack_1.png-2890e21cb51c37827ac42bbea3c02092.ctex", "b748132e8ae5b316ae0fd186c752582af9de52b44e2218d3d5ee0f1acb17b60e", new Vector2I(92, 78), new Rect2(12f, 19f, 53f, 59f)),
		["res://assets/Art/leaders/anim/leader_green_attack_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_attack_2.png", "res://.godot/imported/leader_green_attack_2.png-fc5fd8e34737dc852875b55224fc9f02.ctex", "26195bd808ace52cd8735f62658945d184ed23c9f106e96a3ccb4ecbeec6d9f5", new Vector2I(92, 78), new Rect2(19f, 13f, 63f, 65f)),
		["res://assets/Art/leaders/anim/leader_green_attack_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_attack_3.png", "res://.godot/imported/leader_green_attack_3.png-3bf82696ed20d29ab2dc94cb7efdfa91.ctex", "3e57881c2d8b87e872e74208d509c8614d01181e7336be2797d5a2f1b8d73ae4", new Vector2I(92, 78), new Rect2(15f, 19f, 66f, 59f)),
		["res://assets/Art/leaders/anim/leader_green_block_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_block_0.png", "res://.godot/imported/leader_green_block_0.png-c850b8a9d818e1a5deed60fd80553244.ctex", "478de8d4b59837d9b19b28ec53c7effad552712872fc6074b0708b537bd9c1f0", new Vector2I(92, 78), new Rect2(17f, 17f, 60f, 61f)),
		["res://assets/Art/leaders/anim/leader_green_block_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_block_1.png", "res://.godot/imported/leader_green_block_1.png-dbd73607645b9871518e606381e6b4f0.ctex", "337dc3882ac2791d55e1559e6e107a8b6036ed5e30a779e9a3bea5c023793140", new Vector2I(92, 78), new Rect2(14f, 20f, 64f, 58f)),
		["res://assets/Art/leaders/anim/leader_green_block_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_block_2.png", "res://.godot/imported/leader_green_block_2.png-3b5cdc17df6f8fadb09aba8d3913901c.ctex", "bdbaf14dc6f5510737aaf058b6ef0a6a9822f2c29be04bb6ce4e9b88636b8c77", new Vector2I(92, 78), new Rect2(15f, 20f, 62f, 58f)),
		["res://assets/Art/leaders/anim/leader_green_block_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_block_3.png", "res://.godot/imported/leader_green_block_3.png-5826f6e87cec48fa352dfc3f52a5df6a.ctex", "0ed3b17a2f0dd64f7baf153ba7dd49147b8a2b822607fc5c0e2821b85afa9905", new Vector2I(92, 78), new Rect2(14f, 20f, 64f, 58f)),
		["res://assets/Art/leaders/anim/leader_green_idle_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_0.png", "res://.godot/imported/leader_green_idle_0.png-179e320ca26ed3605765a47bf533d5e7.ctex", "ea159c03170f903e973708b0e5b9816278850dcab1aa8104987aac874ab884cb", new Vector2I(92, 78), new Rect2(16f, 16f, 60f, 62f)),
		["res://assets/Art/leaders/anim/leader_green_idle_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_1.png", "res://.godot/imported/leader_green_idle_1.png-56a8af01764db19e1c7e83fc355899ad.ctex", "3ce8d34b9a780a9b83513999e17ad8e12af788816b92403588ccc1bc0d6499e4", new Vector2I(92, 78), new Rect2(16f, 16f, 60f, 62f)),
		["res://assets/Art/leaders/anim/leader_green_idle_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_2.png", "res://.godot/imported/leader_green_idle_2.png-e7df4266cbbad6ddc43e5bf7ca0b7a50.ctex", "4b49f0e626c6fc24e426a7d3e822ac0156ff6bfbfe4e6e02a2a3a740fbf22501", new Vector2I(92, 78), new Rect2(17f, 15f, 59f, 63f)),
		["res://assets/Art/leaders/anim/leader_green_idle_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_3.png", "res://.godot/imported/leader_green_idle_3.png-e40ac99f14a373daa6adc3ff3fb6b894.ctex", "4a44bc9a544b586ba3355577781a0540739695e8109270750b7f215f2ce77a7e", new Vector2I(92, 78), new Rect2(17f, 14f, 59f, 64f)),
		["res://assets/Art/leaders/anim/leader_green_idle_4.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_4.png", "res://.godot/imported/leader_green_idle_4.png-344e11419ef49b57f6b82087a76eb173.ctex", "e155dcda4a64ab863af6236ffbb601f92b53e63b1097b44d1a786bfb23829468", new Vector2I(92, 78), new Rect2(18f, 13f, 58f, 65f)),
		["res://assets/Art/leaders/anim/leader_green_idle_5.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_5.png", "res://.godot/imported/leader_green_idle_5.png-77493e98be97c2e685c8af30c401caa2.ctex", "f58af8609324834156c0e8bcc214d93d7f0c90ddb8f9a79ebaf38c3636653558", new Vector2I(92, 78), new Rect2(18f, 14f, 58f, 64f)),
		["res://assets/Art/leaders/anim/leader_green_idle_6.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_6.png", "res://.godot/imported/leader_green_idle_6.png-4b86ef8b700e4b412fdf889c02d53a9c.ctex", "416ae75ad4523d2d6e01dddac55020ded255d92a2edd98f6b789823ae608a25f", new Vector2I(92, 78), new Rect2(17f, 14f, 59f, 64f)),
		["res://assets/Art/leaders/anim/leader_green_idle_7.png"] = new Metadata("res://assets/Art/leaders/anim/leader_green_idle_7.png", "res://.godot/imported/leader_green_idle_7.png-19cb495ad4d4730a0669adfe1942f7a8.ctex", "ccdd19426a7010ace9a7aa83e9193256e67d36bd783d4bc7ed42bb6f6986eb69", new Vector2I(92, 78), new Rect2(16f, 15f, 60f, 63f)),
		["res://assets/Art/leaders/anim/leader_white_attack_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_attack_0.png", "res://.godot/imported/leader_white_attack_0.png-3b449c70252338d1a67e9f5685fd3a50.ctex", "224b17fc944a5fe1960b426fd61e43378fd97e0e1d8cfe2ebe16e79a105b7e45", new Vector2I(112, 84), new Rect2(36f, 18f, 34f, 65f)),
		["res://assets/Art/leaders/anim/leader_white_attack_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_attack_1.png", "res://.godot/imported/leader_white_attack_1.png-850f130b8d5ced932fb9e706538ba368.ctex", "f86a62106f39eefb6a774d0e9343f2056d341b53fc14c4032d5a5a20fce25e59", new Vector2I(112, 84), new Rect2(9f, 29f, 76f, 54f)),
		["res://assets/Art/leaders/anim/leader_white_attack_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_attack_2.png", "res://.godot/imported/leader_white_attack_2.png-25d1fd5fa191f3455dfbe9373649b168.ctex", "b066caaf427d249c7c014083715124b4834ac385bc81c46e0c1165e93ff9f30e", new Vector2I(112, 84), new Rect2(13f, 29f, 85f, 54f)),
		["res://assets/Art/leaders/anim/leader_white_attack_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_attack_3.png", "res://.godot/imported/leader_white_attack_3.png-bfec31a1a05c2c7fa6d9a586d497fe77.ctex", "4bb5607dda22737640581f9f6b83fc8eb357b4a7a64a67264f6004c3c775f973", new Vector2I(112, 84), new Rect2(13f, 29f, 81f, 54f)),
		["res://assets/Art/leaders/anim/leader_white_block_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_block_0.png", "res://.godot/imported/leader_white_block_0.png-acefba91c7bd4621d9b06305e6e6cbdd.ctex", "410b3b341cdf2a67924ebe7e52cf97ba5ed513471528567d285cd98b5a4371c1", new Vector2I(112, 64), new Rect2(21f, 7f, 72f, 56f)),
		["res://assets/Art/leaders/anim/leader_white_block_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_block_1.png", "res://.godot/imported/leader_white_block_1.png-ba8b780739f79973d92f8f028c7f131f.ctex", "cb58c93ec081fc4b885b1d62cdaeb930db38ee930018ba56021143e98578cc21", new Vector2I(112, 64), new Rect2(15f, 9f, 77f, 54f)),
		["res://assets/Art/leaders/anim/leader_white_block_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_block_2.png", "res://.godot/imported/leader_white_block_2.png-da44cb77386320f3c5b24110c2c6f52b.ctex", "e9928d325814e7dc2de287f0eb0a917be2acc98de3ab1387d82299bdfe9993e0", new Vector2I(112, 64), new Rect2(15f, 9f, 82f, 54f)),
		["res://assets/Art/leaders/anim/leader_white_block_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_block_3.png", "res://.godot/imported/leader_white_block_3.png-15a955e60aff7aae482372fc8bc80d18.ctex", "b2ff5b233ca31a6f4c4cd9c7b3c02dbb888ccc00d4262d3caddda3950d3ad359", new Vector2I(112, 64), new Rect2(19f, 7f, 75f, 56f)),
		["res://assets/Art/leaders/anim/leader_white_idle_0.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_0.png", "res://.godot/imported/leader_white_idle_0.png-4a262e340b840116940250cf37873417.ctex", "4428b97d81f16598e67af1d3cf0784fb60db679b5a1ac1771ac536ce7c1b4b6a", new Vector2I(112, 84), new Rect2(28f, 27f, 65f, 56f)),
		["res://assets/Art/leaders/anim/leader_white_idle_1.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_1.png", "res://.godot/imported/leader_white_idle_1.png-92e4bf4079b86c27c4d981f452c94bea.ctex", "b5007dd88d8f6c1f8ac5a14fd342c1c8b19847f042a551c2b260b31e820391ab", new Vector2I(112, 84), new Rect2(28f, 26f, 65f, 57f)),
		["res://assets/Art/leaders/anim/leader_white_idle_2.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_2.png", "res://.godot/imported/leader_white_idle_2.png-8741172d0c5d5d5413662a20cdb05b17.ctex", "3974769c7072b4c62fb6c60051b6a9a8416de93224bc608324b0db31e5ec4979", new Vector2I(112, 84), new Rect2(28f, 25f, 66f, 58f)),
		["res://assets/Art/leaders/anim/leader_white_idle_3.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_3.png", "res://.godot/imported/leader_white_idle_3.png-23810b218756dc3dd6831a60f4c7bece.ctex", "938b3c78e8e1a3e5cf22b269d69c157d188db8864e600449323c8c5b55c438cb", new Vector2I(112, 84), new Rect2(28f, 24f, 66f, 59f)),
		["res://assets/Art/leaders/anim/leader_white_idle_4.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_4.png", "res://.godot/imported/leader_white_idle_4.png-95c77a67b2e1fcf6f190f1ea8f170099.ctex", "8ecc4be66678f51184f6edfb8e4f44ccf97796ed689032271da6d82ca56d6c52", new Vector2I(112, 84), new Rect2(28f, 23f, 65f, 60f)),
		["res://assets/Art/leaders/anim/leader_white_idle_5.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_5.png", "res://.godot/imported/leader_white_idle_5.png-7f23d118eba40200ceb885062264f2cc.ctex", "23244d42b9b6e93008619985077cdcce52c9bc1b1e04d2d1002f64404c5dfd7a", new Vector2I(112, 84), new Rect2(28f, 22f, 64f, 61f)),
		["res://assets/Art/leaders/anim/leader_white_idle_6.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_6.png", "res://.godot/imported/leader_white_idle_6.png-ad7d2c7c3de725059204248d1bbdf5a5.ctex", "5c1d1fc29e18cd6fdbcbd7e6ff91bcab5dfb1102c63470e99e5581dcc0985d46", new Vector2I(112, 84), new Rect2(28f, 24f, 64f, 59f)),
		["res://assets/Art/leaders/anim/leader_white_idle_7.png"] = new Metadata("res://assets/Art/leaders/anim/leader_white_idle_7.png", "res://.godot/imported/leader_white_idle_7.png-af9340d1af8e2539aed0ce2597180c46.ctex", "e0d794f18d0009403be198904963eaf5e06d215fa807114553679493c31a2a90", new Vector2I(112, 84), new Rect2(28f, 25f, 64f, 58f)),
		["res://assets/Art/leaders/leader_blue.png"] = new Metadata("res://assets/Art/leaders/leader_blue.png", "res://.godot/imported/leader_blue.png-fa662d3e59ada8e505c1fc8426c883b8.ctex", "a0183ff152d85773ba4ed726791deeda640ea43d8034f685455933d5288f53cf", new Vector2I(128, 84), new Rect2(31f, 13f, 65f, 64f)),
		["res://assets/Art/leaders/leader_green.png"] = new Metadata("res://assets/Art/leaders/leader_green.png", "res://.godot/imported/leader_green.png-46a706db9b35dd2dc24404bd59f1a82d.ctex", "d70d253c3ccf504f118dcb3536657895d2d311c06b9509d772619a7a9dbebbe0", new Vector2I(88, 64), new Rect2(15f, 2f, 59f, 62f)),
		["res://assets/Art/leaders/leader_green_48.png"] = new Metadata("res://assets/Art/leaders/leader_green_48.png", "res://.godot/imported/leader_green_48.png-222bab8138484326dd74f989c0fe34a9.ctex", "7561291c4cf3bd172554cf24cf3660b450c73371db4e8c8f8e8d0b6afd8d3b2a", new Vector2I(68, 50), new Rect2(12f, 2f, 45f, 48f)),
		["res://assets/Art/leaders/leader_green_icon.png"] = new Metadata("res://assets/Art/leaders/leader_green_icon.png", "res://.godot/imported/leader_green_icon.png-6d5db9306205afd50466c9c5cd8577f6.ctex", "ec702b67371745b1a4c944e960ea4a239a48a2caf9b5f87a20eee8c28dc6b67e", new Vector2I(32, 32), new Rect2(3f, 3f, 26f, 28f)),
		["res://assets/Art/leaders/leader_white.png"] = new Metadata("res://assets/Art/leaders/leader_white.png", "res://.godot/imported/leader_white.png-6941202e11579458c4ac8d5cf9f6c21c.ctex", "3be87e1d51fb837aba33685143b191b43ce23ba5d2040dd34016632676d13ce8", new Vector2I(112, 64), new Rect2(28f, 7f, 65f, 56f)),
		["res://assets/Art/leaders/leader_white_icon.png"] = new Metadata("res://assets/Art/leaders/leader_white_icon.png", "res://.godot/imported/leader_white_icon.png-d3fe7009b0c9731ff83b0a6d34d65265.ctex", "1b210cde34c5ad1d148e28a826b8c758652fc883a973b1ebe9fd36e2ecb0209c", new Vector2I(32, 32), new Rect2(0f, 1f, 32f, 31f))
	};

	public static int CachedIdentityCount
	{
		get
		{
			lock (Gate)
			{
				return Cache.Count;
			}
		}
	}

	public static int HashReadCount
	{
		get
		{
			lock (Gate)
			{
				return _hashReadCount;
			}
		}
	}

	internal static Vector3 PixelVertex(Rect2 item, Vector2 pixel, float pixelSize)
	{
		return new Vector3((item.Position.X + pixel.X) * pixelSize, (item.End.Y - pixel.Y) * pixelSize, 0f);
	}

	public static bool TryRead(Texture2D? texture, out Metadata metadata)
	{
		VerificationMode mode;
		return TryRead(texture, out metadata, out mode);
	}

	public static bool TryRead(Texture2D? texture, out Metadata metadata, out VerificationMode mode)
	{
		metadata = default;
		mode = VerificationMode.UnsupportedTexture;
		if (!(texture is CompressedTexture2D imported) || !GodotObject.IsInstanceValid(imported))
		{
			return false;
		}
		ulong identity = imported.GetInstanceId();
		string texturePath = imported.ResourcePath;
		string importedPath = imported.LoadPath;
		Vector2I size = new Vector2I(imported.GetWidth(), imported.GetHeight());
		lock (Gate)
		{
			if (Cache.TryGetValue(identity, out var cached))
			{
				if (texturePath != cached.TexturePath || importedPath != cached.ImportedPath || size != cached.TextureSize)
				{
					mode = VerificationMode.IdentityChanged;
					return false;
				}
				mode = (cached.Verified ? VerificationMode.VerifiedCacheHit : VerificationMode.RejectedCacheHit);
				if (cached.Verified)
				{
					metadata = cached.Metadata;
				}
				return cached.Verified;
			}
			if (Cache.Count >= 256)
			{
				mode = VerificationMode.CacheCapacityReached;
				return false;
			}
			bool verified = false;
			Metadata expected = default;
			if (!Table.TryGetValue(texturePath, out expected))
			{
				mode = VerificationMode.UnknownTexture;
			}
			else if (!string.Equals(importedPath, expected.ImportedPath, StringComparison.Ordinal))
			{
				mode = VerificationMode.ImportedPathMismatch;
			}
			else if (size != expected.TextureSize)
			{
				mode = VerificationMode.DimensionMismatch;
			}
			else
			{
				_hashReadCount++;
				string actualHash = FileAccess.GetSha256(importedPath);
				if (string.IsNullOrEmpty(actualHash))
				{
					mode = VerificationMode.HashUnavailable;
				}
				else if (!string.Equals(actualHash, expected.Sha256, StringComparison.OrdinalIgnoreCase))
				{
					mode = VerificationMode.HashMismatch;
				}
				else
				{
					verified = true;
					mode = VerificationMode.VerifiedFirstRead;
				}
			}
			Cache.Add(identity, new Verification(texturePath, importedPath, size, verified, verified ? expected : default(Metadata)));
			if (verified)
			{
				metadata = expected;
			}
			return verified;
		}
	}
}
