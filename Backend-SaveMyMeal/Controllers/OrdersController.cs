using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Backend_SaveMyMeal.Models.DTOs.Orders;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Si l'utilisateur est authentifié via JWT et n'a pas spécifié de ClientId ou pour sécuriser
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int clientId = dto.ClientId;
            if (clientId <= 0 && !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var claimId))
            {
                clientId = claimId;
            }

            if (clientId <= 0)
            {
                return BadRequest(new { message = "L'identifiant du client (ClientId) est obligatoire." });
            }

            try
            {
                var createdOrder = await _orderService.CreateOrderAsync(clientId, dto);
                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erreur lors de la création de la commande.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Commande avec l'ID {id} introuvable." });
            }

            return Ok(order);
        }

        [HttpGet("user/{clientId}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByClient(int clientId)
        {
            var orders = await _orderService.GetOrdersByClientAsync(clientId);
            return Ok(orders);
        }

        [HttpGet("restaurant/{restaurantId}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByRestaurant(int restaurantId)
        {
            var orders = await _orderService.GetOrdersByRestaurantAsync(restaurantId);
            return Ok(orders);
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            if (updatedOrder == null)
            {
                return NotFound(new { message = $"Commande avec l'ID {id} introuvable." });
            }

            return Ok(updatedOrder);
        }

        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "Client";
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdClaim)) int.TryParse(userIdClaim, out userId);

            try
            {
                var success = await _orderService.CancelOrderAsync(id, userId, roleClaim);
                if (!success)
                {
                    return NotFound(new { message = $"Commande avec l'ID {id} introuvable." });
                }

                return Ok(new { message = "Commande annulée avec succès et stocks réapprovisionnés." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erreur lors de l'annulation.", details = ex.Message });
            }
        }
    }
}
