using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.QrCodes;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface IQrCodeService
    {
        Task<QrCode> GenerateQrCodeAsync(int orderId);
        Task<QrValidationResponseDto> ValidateQrCodeAsync(string codeValue, int? restaurantId = null);
        Task<QrCode?> GetQrCodeByOrderIdAsync(int orderId);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly AppDbContext _context;

        public QrCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QrCode> GenerateQrCodeAsync(int orderId)
        {
            var existing = await _context.QrCodes.FirstOrDefaultAsync(q => q.OrderId == orderId);
            if (existing != null)
            {
                return existing;
            }

            var uniqueSuffix = RandomNumberGenerator.GetHexString(6).ToUpperInvariant();
            var codeValue = $"SMM-ORD-{orderId:D6}-{uniqueSuffix}";

            var qrCode = new QrCode
            {
                OrderId = orderId,
                CodeValue = codeValue,
                IsUsed = false,
                GeneratedAt = DateTime.UtcNow
            };

            _context.QrCodes.Add(qrCode);
            await _context.SaveChangesAsync();

            return qrCode;
        }

        public async Task<QrValidationResponseDto> ValidateQrCodeAsync(string codeValue, int? restaurantId = null)
        {
            var normalizedCode = codeValue.Trim().ToUpperInvariant();

            var qrCode = await _context.QrCodes
                .Include(q => q.Order)
                    .ThenInclude(o => o!.Client)
                .Include(q => q.Order)
                    .ThenInclude(o => o!.Restaurant)
                .Include(q => q.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Meal)
                .FirstOrDefaultAsync(q => q.CodeValue.ToUpper() == normalizedCode);

            if (qrCode == null || qrCode.Order == null)
            {
                return new QrValidationResponseDto
                {
                    IsValid = false,
                    Message = "Code QR invalide ou introuvable."
                };
            }

            if (qrCode.IsUsed)
            {
                return new QrValidationResponseDto
                {
                    IsValid = false,
                    Message = "Ce panier a déjà été récupéré et ce code QR a déjà été consommé.",
                    OrderId = qrCode.OrderId,
                    Status = qrCode.Order.Status
                };
            }

            if (restaurantId.HasValue && qrCode.Order.RestaurantId != restaurantId.Value)
            {
                return new QrValidationResponseDto
                {
                    IsValid = false,
                    Message = $"Erreur : Ce code QR correspond au restaurant '{qrCode.Order.Restaurant?.Name}' et non au vôtre.",
                    OrderId = qrCode.OrderId
                };
            }

            if (qrCode.Order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return new QrValidationResponseDto
                {
                    IsValid = false,
                    Message = "Cette commande a été annulée. Retrait impossible.",
                    OrderId = qrCode.OrderId,
                    Status = "Cancelled"
                };
            }

            // Marquer le QR code comme consommé et la commande comme terminée
            qrCode.IsUsed = true;
            qrCode.Order.Status = "Completed";

            await _context.SaveChangesAsync();

            var itemsSummary = qrCode.Order.OrderItems
                .Select(oi => $"{oi.Quantity}x {oi.Meal?.Title ?? "Plat"}")
                .ToList();

            return new QrValidationResponseDto
            {
                IsValid = true,
                Message = "Panier validé avec succès ! Remettez le repas au client.",
                OrderId = qrCode.OrderId,
                ClientUsername = qrCode.Order.Client?.Username,
                RestaurantName = qrCode.Order.Restaurant?.Name,
                TotalPrice = qrCode.Order.TotalPrice,
                Status = qrCode.Order.Status,
                ValidatedAt = DateTime.UtcNow,
                ItemsSummary = itemsSummary
            };
        }

        public async Task<QrCode?> GetQrCodeByOrderIdAsync(int orderId)
        {
            return await _context.QrCodes
                .Include(q => q.Order)
                .FirstOrDefaultAsync(q => q.OrderId == orderId);
        }
    }
}
