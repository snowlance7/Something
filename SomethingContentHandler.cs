using Dusk;
using UnityEngine;

namespace Something
{
    public class SomethingContentHandler : ContentHandler<SomethingContentHandler>
    {
        public class AubreyPlushAssets(DuskMod mod, string filePath) : AssetBundleLoader<AubreyPlushAssets>(mod, filePath) { }
        public class BasilPlushAssets(DuskMod mod, string filePath) : AssetBundleLoader<BasilPlushAssets>(mod, filePath) { }
        public class BunnybunAssets(DuskMod mod, string filePath) : AssetBundleLoader<BunnybunAssets>(mod, filePath) { }
        public class KeytarAssets(DuskMod mod, string filePath) : AssetBundleLoader<KeytarAssets>(mod, filePath) { }
        public class MailboxAssets(DuskMod mod, string filePath) : AssetBundleLoader<MailboxAssets>(mod, filePath) { }
        public class PolaroidAssets(DuskMod mod, string filePath) : AssetBundleLoader<PolaroidAssets>(mod, filePath) { }
        public class RabbitAssets(DuskMod mod, string filePath) : AssetBundleLoader<RabbitAssets>(mod, filePath) { }
        public class SomethingAssets(DuskMod mod, string filePath) : AssetBundleLoader<SomethingAssets>(mod, filePath)
        {
            [LoadFromBundle("Something.prefab")]
            public GameObject SomethingPrefab { get; private set; } = null!;
        }

        public AubreyPlushAssets? AubreyPlush;
        public BasilPlushAssets? BasilPlush;
        public BunnybunAssets? Bunnybun;
        public KeytarAssets? Keytar;
        public MailboxAssets? Mailbox;
        public PolaroidAssets? Polaroid;
        public RabbitAssets? Rabbit;
        public SomethingAssets? Something;

        public SomethingContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("aubreyplush", out AubreyPlush);
            RegisterContent("basilplush", out BasilPlush);
            RegisterContent("bunnybun", out Bunnybun);
            RegisterContent("keytar", out Keytar);
            RegisterContent("mailbox", out Mailbox);
            RegisterContent("polaroid", out Polaroid);
            RegisterContent("rabbit", out Rabbit);
            RegisterContent("something", out Something);
        }
    }
}
/*public class DuckSongAssets(DuskMod mod, string filePath) : AssetBundleLoader<DuckSongAssets>(mod, filePath)
{
    [LoadFromBundle("DuckHolder.prefab")]
    public GameObject DuckUIPrefab { get; private set; } = null!;
}*/