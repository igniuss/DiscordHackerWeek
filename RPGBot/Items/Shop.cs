using System.Linq;

namespace RPGBot.Items {

    public class Shop {
        public static ItemBase[] Items = ItemBase.GetAllItems().ToArray();

        public static string GetShopDescription() {
            return "Use the command ``buy <item> <amount>`` to purchase an item.\n" +
                string.Join<ItemBase>("\n\n", Items);
        }
    }
}