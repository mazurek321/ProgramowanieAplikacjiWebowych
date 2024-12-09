using projekt.src.Models.Store;
using projekt.src.Models.Users;

namespace projekt.src.Models.ShoppingCart;

public class ShoppingCart
{
    private ShoppingCart(Guid id, UserId ownerId)
    {
        Id = id;
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    private ShoppingCart(Guid id, UserId ownerId, List<ShoppingCartItem> items, DateTime updatedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Items = items;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public UserId OwnerId { get; private set; }
    public List<ShoppingCartItem> Items {get; private set; } = new List<ShoppingCartItem>();
    public DateTime UpdatedAt { get; private set; }

    public static ShoppingCart New(UserId ownerId)
    {
        return new ShoppingCart(Guid.NewGuid(), ownerId);
    }

    public ShoppingCartItem AddItem(Guid announcementId, Quantity quantityVal)
    {
        
        var quantity = new Quantity(quantityVal.Value);
        var cartItem = ShoppingCartItem.NewItem(Id, announcementId, quantity); 
        UpdatedAt = DateTime.UtcNow;
        Items.Add(cartItem);
        return cartItem;
    }

    public void UpdateItem(Guid announcementId, Quantity quantityVal)
    {   
        var quantity = new Quantity(quantityVal.Value);
        var cartItem = Items.FindIndex(x=>x.AnnouncementId == announcementId);

        UpdatedAt = DateTime.UtcNow;
        Items[cartItem].Update(quantity);
    }

    public void ClearCart()
    {
        UpdatedAt = DateTime.UtcNow;
        Items.Clear();
    }

    public void RemoveOneItem(Announcement item)
    {
        UpdatedAt = DateTime.UtcNow;
        var index = Items.FindIndex(x=>x.AnnouncementId == item.Id);
        Items.RemoveAt(index);
    } 
}