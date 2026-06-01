using Dawn.Utils;
using Dusk;

namespace Something
{
    internal static class Configs
    {
        // Polaroid
        public static int goodWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Good Weight").Value; // 0
        public static int badWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Bad Weight").Value; // 1
        public static int cursedWeight => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<int>("Cursed Weight").Value; // 2
        public static BoundedRange goodValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Good Value").Value;
        public static BoundedRange badValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Bad Value").Value;
        public static BoundedRange cursedValue => ContentHandler<SomethingContentHandler>.Instance.Polaroid!.GetConfig<BoundedRange>("Cursed Value").Value;
    }
}
