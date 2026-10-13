// init-only プロパティ (C# 9 record) を Unity 2022.3 (.NET Standard 2.1) で使うためのポリフィル。
// .NET 5+ (EngineTests の net8.0) は BCL が持つので二重定義しない
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
