using Microsoft.AspNetCore.Mvc;
using Backend_SaveMyMeal.Models.DTOs.QrCodes;
using Backend_SaveMyMeal.Services;

namespace Backend_SaveMyMeal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QrCodesController : ControllerBase
    {
        private readonly IQrCodeService _qrCodeService;

        public QrCodesController(IQrCodeService qrCodeService)
        {
            _qrCodeService = qrCodeService;
        }

        [HttpGet("order/{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetQrCodeByOrderId(int orderId)
        {
            var qrCode = await _qrCodeService.GetQrCodeByOrderIdAsync(orderId);
            if (qrCode == null)
            {
                return NotFound(new { message = $"Aucun QR code trouvé pour la commande {orderId}." });
            }

            return Ok(new
            {
                id = qrCode.Id,
                orderId = qrCode.OrderId,
                codeValue = qrCode.CodeValue,
                isUsed = qrCode.IsUsed,
                generatedAt = qrCode.GeneratedAt
            });
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(QrValidationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateQrCode([FromBody] QrValidationRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _qrCodeService.ValidateQrCodeAsync(dto.CodeValue, dto.RestaurantId);
            if (!result.IsValid)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
