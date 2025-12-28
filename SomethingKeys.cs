using Dawn;
using System;
using System.Collections.Generic;
using System.Text;

namespace Something
{
    public static class SomethingKeys
    {
        public const string Namespace = "something";

        internal static NamespacedKey LastVersion = NamespacedKey.From(Namespace, "last_version");

        public static readonly NamespacedKey<DawnEnemyInfo> Something = NamespacedKey<DawnEnemyInfo>.From("something", "something");
        public static readonly NamespacedKey<DawnEnemyInfo> Rabbit = NamespacedKey<DawnEnemyInfo>.From("something", "rabbit");

        public static readonly NamespacedKey<DawnItemInfo> AubreyPlush = NamespacedKey<DawnItemInfo>.From("something", "aubrey_plush");
        public static readonly NamespacedKey<DawnItemInfo> BasilPlush = NamespacedKey<DawnItemInfo>.From("something", "basil_plush");
        public static readonly NamespacedKey<DawnItemInfo> Bunnybun = NamespacedKey<DawnItemInfo>.From("something", "bunnybun");
        public static readonly NamespacedKey<DawnItemInfo> Keytar = NamespacedKey<DawnItemInfo>.From("something", "keytar");
        public static readonly NamespacedKey<DawnItemInfo> Mailbox = NamespacedKey<DawnItemInfo>.From("something", "mailbox");
        public static readonly NamespacedKey<DawnItemInfo> Polaroid = NamespacedKey<DawnItemInfo>.From("something", "polaroid");
    }
}
