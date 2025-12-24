using Dusk;
using UnityEngine;

namespace Something
{
    public class SomethingContentHandler : ContentHandler<SomethingContentHandler>
    {
        public class SomethingAssets(DuskMod mod, string filePath) : AssetBundleLoader<SomethingAssets>(mod, filePath) { }
        public class AubreyPlushAssets(DuskMod mod, string filePath) : AssetBundleLoader<AubreyPlushAssets>(mod, filePath) { }
        public class BasilPlushAssets(DuskMod mod, string filePath) : AssetBundleLoader<BasilPlushAssets>(mod, filePath) { }
        public class BunnybunAssets(DuskMod mod, string filePath) : AssetBundleLoader<BunnybunAssets>(mod, filePath) { }
        public class KeytarAssets(DuskMod mod, string filePath) : AssetBundleLoader<KeytarAssets>(mod, filePath) { }
        public class MailboxAssets(DuskMod mod, string filePath) : AssetBundleLoader<MailboxAssets>(mod, filePath) { }
        public class GoodPolaroidAssets(DuskMod mod, string filePath) : AssetBundleLoader<GoodPolaroidAssets>(mod, filePath) { }
        public class BadPolaroidAssets(DuskMod mod, string filePath) : AssetBundleLoader<BadPolaroidAssets>(mod, filePath) { }
        public class CursedPolaroidAssets(DuskMod mod, string filePath) : AssetBundleLoader<CursedPolaroidAssets>(mod, filePath) { }
        public class RabbitAssets(DuskMod mod, string filePath) : AssetBundleLoader<RabbitAssets>(mod, filePath) { }

        public SomethingAssets? Something;
        public AubreyPlushAssets? AubreyPlush;
        public BasilPlushAssets? BasilPlush;
        public BunnybunAssets? Bunnybun;
        public KeytarAssets? Keytar;
        public MailboxAssets? Mailbox;
        public GoodPolaroidAssets? GoodPolaroid;
        public BadPolaroidAssets? BadPolaroid;
        public CursedPolaroidAssets? CursedPolaroid;
        public RabbitAssets? Rabbit;

        public SomethingContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("somethingassets", out Something);
            RegisterContent("aubreyplushassets", out AubreyPlush);
            RegisterContent("basilplushassets", out BasilPlush);
            RegisterContent("bunnybunassets", out Bunnybun);
            RegisterContent("keytarassets", out Keytar);
            RegisterContent("mailboxassets", out Mailbox);
            RegisterContent("goodpolaroidassets", out GoodPolaroid);
            RegisterContent("badpolaroidassets", out BadPolaroid);
            RegisterContent("cursedpolaroidassets", out CursedPolaroid);
            RegisterContent("rabbitassets", out Rabbit);
        }
    }
}