using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Core.Entities;
using OmniRoute.Infrastructure.Data;

namespace OmniRoute.Api.Controllers
{
    public class BatchSendRequest
    {
        public List<string> Recipients { get; set; } = new List<string>();
        public string? MessageText { get; set; }
        public string? Channel { get; set; } = "SMS"; // SMS, WhatsApp, Viber
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = "Delivered";
    }

    [ApiController]
    [Route("api/v1/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public MessagesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 1. सबै मेसेज लग्सहरू देखाउनको लागि नयाँ थपिएको GET endpoint
        [HttpGet]
        public async Task<IActionResult> GetAllMessages()
        {
            try
            {
                var messages = await _dbContext.MessageLogs
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(50) // पछिल्लो ५० वटा मात्र देखाउने
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, error = innerMsg });
            }
        }

        [HttpPost("batch-send")]
        public async Task<IActionResult> BatchSend([FromBody] BatchSendRequest request)
        {
            if (request == null || request.Recipients == null || !request.Recipients.Any())
            {
                return BadRequest(new { success = false, error = "Recipients list cannot be empty." });
            }

            try
            {
                foreach (var recipient in request.Recipients)
                {
                    string channel = string.IsNullOrWhiteSpace(request.Channel) ? "SMS" : request.Channel;

                    var messageLog = new MessageLog
                    {
                        Id = Guid.NewGuid(),
                        DestinationNumber = recipient,
                        DeliveryStatus = "Pending",
                        DispatchedChannel = channel,
                        CostCharged = 0.00m,
                        CreatedAt = DateTime.UtcNow,
                        RequestedParametersJson = request.MessageText
                    };

                    _dbContext.MessageLogs.Add(messageLog);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"{request.Recipients.Count} message(s) queued successfully via {request.Channel}."
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, error = innerMsg });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingMessages([FromQuery] string? channel)
        {
            try
            {
                var query = _dbContext.MessageLogs.Where(m => m.DeliveryStatus == "Pending");

                if (!string.IsNullOrWhiteSpace(channel))
                {
                    query = query.Where(m => m.DispatchedChannel == channel);
                }

                var pendingMessages = await query
                    .OrderBy(m => m.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return Ok(pendingMessages);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, error = innerMsg });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var log = await _dbContext.MessageLogs.FindAsync(id);
                if (log != null)
                {
                    log.DeliveryStatus = request.Status;
                    log.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, error = innerMsg });
            }
        }
    }
}