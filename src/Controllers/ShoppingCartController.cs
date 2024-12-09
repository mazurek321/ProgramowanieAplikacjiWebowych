using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt.src.Controllers.Dto.ShoppingCartDto;
using projekt.src.Data;
using projekt.src.Exceptions;
using projekt.src.Models.ShoppingCart;

namespace projekt.src.Controllers;

[ApiController]
[Route("[controller]")]
public class ShoppingCartController : ControllerBase
{
    private readonly ApiDbContext _dbContext;
    private readonly UserService _userService;

    public ShoppingCartController(
        ApiDbContext dbContext,
        UserService userService
    )
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    [NonAction]
    public async Task <ShoppingCart> CreateCart()
    {
        var user = await _userService.CurrentUser(User);
        var cartExists = await GetCart();

        if(cartExists is null){
            var cart = ShoppingCart.New(user.Id);

            _dbContext.ShoppingCarts.Add(cart);
            await _dbContext.SaveChangesAsync();
            
            return cart;
        }

        return cartExists;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddItem(
        [FromQuery] Guid AnnouncementId,
        [FromBody] AddItemDto dto
    )
    {
        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == AnnouncementId);
        if (announcement is null)
            throw new CustomException("Announcement not found.");

        var user = await _userService.CurrentUser(User);
        var cart = await CreateCart();
        if (cart is null)
            throw new CustomException("Cart not found.");

        var quantity = new Quantity(dto.Quantity);

        if(dto.Quantity <= 0) throw new CustomException("Invalid amount.");

        var totalAvailableQuantity = announcement.Item.Amount.Value;
        if (quantity.Value > totalAvailableQuantity)
            throw new CustomException("The selected quantity exceeds the available amount of the item.");


         var existingItem = cart.Items.FirstOrDefault(x => x.AnnouncementId == announcement.Id);

        if (existingItem != null)
        {
            existingItem.Update(quantity);
            _dbContext.ShoppingCartItems.Update(existingItem);
        }
        else
        {
            var cartItem = ShoppingCartItem.NewItem(cart.Id, AnnouncementId, quantity);
            _dbContext.ShoppingCartItems.Add(cartItem);
        }

        await _dbContext.SaveChangesAsync();
        return Ok(cart);
    }


    [HttpPut]
    [Authorize]
    public async Task <IActionResult> UpdateItem(
        [FromQuery] Guid AnnouncementId,
        [FromBody] AddItemDto dto)
    {
        var user = await _userService.CurrentUser(User);
        var cart = await GetCart();
        
        var existingItem = cart.Items.FirstOrDefault(x=>x.AnnouncementId == AnnouncementId);

        if(existingItem is null) throw new CustomException("Item not found.");

        var quantity = new Quantity(dto.Quantity);

        if(dto.Quantity <= 0) throw new CustomException("Invalid amount.");

        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == AnnouncementId);
        if (announcement is null)
            throw new CustomException("Announcement not found.");

        var totalAvailableQuantity = announcement.Item.Amount.Value;
        if (quantity.Value > totalAvailableQuantity)
            throw new CustomException("The selected quantity exceeds the available amount of the item.");

        cart.UpdateItem(AnnouncementId, quantity);

        await _dbContext.SaveChangesAsync();

        return Ok(cart);
    }


    [HttpGet]
    [Authorize]
    public async Task <ShoppingCart> GetCart()
    {
        var user = await _userService.CurrentUser(User);
        var cart = await _dbContext.ShoppingCarts
                            .Include(x=>x.Items)
                            .FirstOrDefaultAsync(x=>x.OwnerId == user.Id);

        return cart;
    }

    [HttpDelete("clear")]
    [Authorize]
    public async Task<IActionResult> ClearCart()
    {
        var cart = await GetCart();

        if(cart is null) throw new CustomException("Cart not found.");

        _dbContext.ShoppingCartItems.RemoveRange(cart.Items);
        cart.ClearCart();
        await _dbContext.SaveChangesAsync();

        return Ok();

    }
}
