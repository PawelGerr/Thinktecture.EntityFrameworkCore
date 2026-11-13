global using Lock =
#if NET9_0_OR_GREATER
   System.Threading.Lock;
#else
   System.Object;
#endif
