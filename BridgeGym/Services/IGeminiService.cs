using System.Collections.Generic;
using System.Threading.Tasks;
using BridgeGym.Models.Bridge;

namespace BridgeGym.Services;

public interface IGeminiService
{
    Task<List<Card>?> ParseHandImageAsync(byte[] imageBytes);
}
