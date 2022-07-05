using GeekShopping.CartAPI.Data.ValueObjects;
using GeekShopping.CartAPI.Messages;
using GeekShopping.CartAPI.RabbitMqSender;
using GeekShopping.CartAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace GeekShopping.CartAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly IRabbitMqMessageSender _rabbitMqMessageSender;

        public CartController(ICartRepository cartRepository, ICouponRepository couponRepository, IRabbitMqMessageSender rabbitMqMessageSender)
        {
            _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
            _rabbitMqMessageSender = rabbitMqMessageSender ?? throw new ArgumentNullException(nameof(rabbitMqMessageSender));
        }

        [HttpGet("find-cart/{id}")]
        public async Task<ActionResult<CartVO>> FindById(string id)
        {
            var cart = await _cartRepository.FindCartByUserId(id);
            if (cart == null)
                return NotFound();

            return Ok(cart);
        }
        
        [HttpPost("add-cart")]
        public async Task<ActionResult<CartVO>> AddCart(CartVO vo)
        {
            var cart = await _cartRepository.SaveOrUpdateCart(vo);
            if (cart == null)
                return NotFound();

            return Ok(cart);
        }
        
        [HttpPut("update-cart")]
        public async Task<ActionResult<CartVO>> UpdateCart(CartVO vo)
        {
            var cart = await _cartRepository.SaveOrUpdateCart(vo);
            if (cart == null)
                return NotFound();

            return Ok(cart);
        }
        
        [HttpDelete("remove-cart/{id}")]
        public async Task<ActionResult<CartVO>> RemoveCart(int id)
        {
            var status = await _cartRepository.RemoveFromCart(id);
            if (!status)
                return BadRequest();

            return Ok(status);
        }

        [HttpPost("apply-coupon")]
        public async Task<ActionResult<CartVO>> ApplyCoupon(CartVO vo)
        {
            var status = await _cartRepository.ApplyCoupon(vo.CartHeader.UserId, vo.CartHeader.CouponCode);
            if (!status)
                return NotFound();

            return Ok(status);
        }
        
        [HttpDelete("remove-coupon/{userId}")]
        public async Task<ActionResult<CartVO>> RemoveCoupon(string userId)
        {
            var status = await _cartRepository.RemoveCoupon(userId);
            if (!status)
                return NotFound();

            return Ok(status);
        }
        
        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutHeaderVO>> Checkout(CheckoutHeaderVO vo)
        {
            string token = Request.Headers["Authorization"];

            if (vo?.UserId == null)
                return BadRequest();

            var cart = await _cartRepository.FindCartByUserId(vo.UserId);
            if (cart == null)
                return NotFound();

            if (!string.IsNullOrEmpty(vo.CouponCode))
            {
                CouponVO coupon = await _couponRepository.GetCoupon(vo.CouponCode, token);
                
                if (vo.DiscountAmount != coupon.DiscountAmount)
                {
                    return StatusCode(412);
                }
            }

            vo.CartDetails = cart.CartDetails;
            foreach (var cartDetail in vo.CartDetails)
            {
                cartDetail.CartHeader = cart.CartHeader;
            }
            vo.DateTime = DateTime.Now;

            _rabbitMqMessageSender.SendMessage(vo, "checkoutqueue");

            return Ok(vo);
        }
    }
}