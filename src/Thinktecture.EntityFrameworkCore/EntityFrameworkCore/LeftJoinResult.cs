namespace Thinktecture.EntityFrameworkCore
{
   public class LeftJoinResult<TOuter, TInner>
   {
      public TOuter Outer { get; set; }
      public TInner Inner { get; set; }
   }
}
