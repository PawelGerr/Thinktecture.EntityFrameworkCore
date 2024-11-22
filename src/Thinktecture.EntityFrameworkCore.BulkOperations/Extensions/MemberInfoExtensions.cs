using System.Reflection;
using System.Runtime.CompilerServices;

namespace Thinktecture;

internal static class MemberInfoExtensions
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static bool IsEqualTo(this MemberInfo member, MemberInfo? other)
   {
      return other is not null
             && member.MetadataToken == other.MetadataToken
             && ReferenceEquals(member.Module, other.Module);
   }
}
