// Hd2dMath.cs — dot の Godot 版 (codex/unity-parity-20261013・checkpoint 192) の演出コードを「そのまま」Unity で読むための、Godot 風のベクトル (2026-10-27)。
// 目的: Hd2dAttackPrototype.cs ほかを一字一句近い形で持ってくる (dot が Godot で続きを作っても差分で追える)。
// Godot の Vector3/Vector2 は X・Y・Z がフィールドで、Normalized()・Lerp()・DistanceTo() がメソッド。UnityEngine の型とは暗黙に相互変換できる。
// Godot の座標系の向き (Back = +Z = 画面の手前) も値のまま。世界への写しは Hd2dAttackFx が行う (カメラの右・上・手前の基底)。
using System;
namespace DeckRogue.Game.Hd2dMath
{
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static readonly Vector3 Zero = new Vector3(0, 0, 0), One = new Vector3(1, 1, 1);
        public static readonly Vector3 Up = new Vector3(0, 1, 0), Down = new Vector3(0, -1, 0);
        public static readonly Vector3 Left = new Vector3(-1, 0, 0), Right = new Vector3(1, 0, 0);
        public static readonly Vector3 Forward = new Vector3(0, 0, -1), Back = new Vector3(0, 0, 1);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.X, -a.Y, -a.Z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator *(float s, Vector3 a) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator *(Vector3 a, Vector3 b) => new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.X / s, a.Y / s, a.Z / s);
        public static Vector3 operator /(Vector3 a, Vector3 b) => new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared() => X * X + Y * Y + Z * Z;
        /// <summary>Godot と同じく、長さ 0 なら零ベクトル</summary>
        public Vector3 Normalized() { float l = LengthSquared(); if (l <= 0f) return Zero; l = MathF.Sqrt(l); return new Vector3(X / l, Y / l, Z / l); }
        public Vector3 Lerp(Vector3 to, float w) => new Vector3(X + (to.X - X) * w, Y + (to.Y - Y) * w, Z + (to.Z - Z) * w);
        public float DistanceTo(Vector3 b) => (b - this).Length();
        public float DistanceSquaredTo(Vector3 b) => (b - this).LengthSquared();
        public float Dot(Vector3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public Vector3 Cross(Vector3 b) => new Vector3(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
        public bool IsFinite() => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
        public static implicit operator UnityEngine.Vector3(Vector3 v) => new UnityEngine.Vector3(v.X, v.Y, v.Z);
        public static implicit operator Vector3(UnityEngine.Vector3 v) => new Vector3(v.x, v.y, v.z);
        public override string ToString() => "(" + X + ", " + Y + ", " + Z + ")";
    }

    public struct Vector2
    {
        public float X, Y;
        public Vector2(float x, float y) { X = x; Y = y; }
        public static readonly Vector2 Zero = new Vector2(0, 0), One = new Vector2(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.X, -a.Y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.X * s, a.Y * s);
        public static Vector2 operator *(float s, Vector2 a) => new Vector2(a.X * s, a.Y * s);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.X * b.X, a.Y * b.Y);
        public static Vector2 operator /(Vector2 a, float s) => new Vector2(a.X / s, a.Y / s);
        public float Length() => MathF.Sqrt(X * X + Y * Y);
        public float LengthSquared() => X * X + Y * Y;
        public Vector2 Normalized() { float l = LengthSquared(); if (l <= 0f) return Zero; l = MathF.Sqrt(l); return new Vector2(X / l, Y / l); }
        public Vector2 Lerp(Vector2 to, float w) => new Vector2(X + (to.X - X) * w, Y + (to.Y - Y) * w);
        public float DistanceTo(Vector2 b) => (b - this).Length();
        public bool IsFinite() => float.IsFinite(X) && float.IsFinite(Y);
        public static implicit operator UnityEngine.Vector2(Vector2 v) => new UnityEngine.Vector2(v.X, v.Y);
        public static implicit operator Vector2(UnityEngine.Vector2 v) => new Vector2(v.x, v.y);
        public override string ToString() => "(" + X + ", " + Y + ")";
    }
}
